using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sequel.Models;
using static Sequel.Helper;

namespace Sequel.Core
{
    public static class QueryManager
    {
        private static readonly Dictionary<string, CancellationTokenSource> TokensByQueryId = new Dictionary<string, CancellationTokenSource>();

        public static async Task<QueryResponseContext> ExecuteQuery(QueryExecutionContext context)
        {
            string queryId = Check.NotNull(context.Id, nameof(context.Id));
            var cancellationToken = CreateToken(queryId);

            try
            {
                return await context.ExecuteQuery(cancellationToken);
            }
            finally
            {
                ReleaseToken(queryId);
            }
        }

        public static void Cancel(string queryId)
        {
            if (TokensByQueryId.TryGetValue(queryId, out var cts))
            {
                try
                {
                    cts.Cancel();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to cancel query execution: {ex.Message}");
                }
                finally
                {
                    ReleaseToken(queryId);
                }
            }
        }

        private static async Task<QueryResponseContext> ExecuteQuery(this QueryExecutionContext context, CancellationToken cancellationToken)
        {
            return await context.Server.Execute(context.Database, context.GetSqlStatement()!, async (dbCommand, ct) =>
            {
                var response = new QueryResponseContext(context.Id);
                var sw = new Stopwatch();

                try
                {
                    sw.Start();
                    using var ctr = ct.Register(() => dbCommand.Cancel());
                    using var dataReader = await dbCommand.ExecuteReaderAsync(ct);

                    do
                    {
                        var columnNames = new List<string>();
                        response.Columns.Clear();
                        response.Rows.Clear();

                        for (int i = 0; i < dataReader.FieldCount; i++)
                        {
                            columnNames.Add(columnNames.Contains(dataReader.GetName(i))
                                ? dataReader.GetName(i) + (i + 1)
                                : dataReader.GetName(i));

                            response.Columns.Add(new ColumnDefinition(columnNames[i], dataReader.GetDataTypeName(i)));
                        }

                        while (await dataReader.ReadAsync(ct))
                        {
                            var dataRow = new ExpandoObject() as IDictionary<string, object?>;
                            for (int i = 0; i < dataReader.FieldCount; i++)
                            {
                                var value = dataReader[i];
                                dataRow.Add(columnNames[i], value is DBNull ? null : value);
                            }
                            response.Rows.Add(dataRow);
                        }
                    } while (await dataReader.NextResultAsync(ct));

                    response.RecordsAffected = dataReader.RecordsAffected;
                }
                catch (Exception ex)
                {
                    if (ct.IsCancellationRequested)
                    {
                        response.Status = QueryResponseStatus.Canceled;
                        response.Columns.Clear();
                        response.Rows.Clear();
                    }
                    else
                    {
                        response.Status = QueryResponseStatus.Failed;
                        response.Error = ex.Message;
                        if (int.TryParse(ex.Data["Position"]?.ToString(), out int position))
                        {
                            response.ErrorPosition = position;
                        }
                    }
                }
                finally
                {
                    response.Elapsed = sw.ElapsedMilliseconds;
                    await IgnoreErrorsAsync(() => History.Save(context, response));
                }

                return response;
            }, dbCommand => dbCommand.CommandTimeout = 0, cancellationToken);
        }

        private static CancellationToken CreateToken(string queryId)
        {
            if (TokensByQueryId.ContainsKey(queryId))
            {
                throw new Exception("A query is already being executed.");
            }

            var cts = new CancellationTokenSource();
            TokensByQueryId[queryId] = cts;
            return cts.Token;
        }

        private static bool ReleaseToken(string queryId)
        {
            if (TokensByQueryId.TryGetValue(queryId, out var cts))
            {
                cts.Dispose();
                return TokensByQueryId.Remove(queryId);
            }

            return false;
        }

        public static class History
        {
            private static readonly char[] CharsToTrimStart = { '\r', '\n' };
            private static readonly char[] CharsToTrimEnd = { '\r', '\n', '\t', ' ' };
            private static readonly ServerConnection ServerConnection = new ServerConnection
            {
                Name = "QueryHistory Sqlite database connection",
                Type = DBMS.SQLite,
                ConnectionString = $@"Data Source={Path.Combine(Program.RootDirectory, typeof(QueryHistory).Name.ToLower() + ".db")};"
            };

            public static async Task Configure()
            {
                if (await ServerConnection.QueryForLong("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND tbl_name = 'data'") == 1)
                {
                    await ServerConnection.ExecuteNonQuery("DROP TABLE [data]");
                }

                if (await ServerConnection.QueryForLong("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND tbl_name = 'query'") == 0)
                {
                    string sql =
                    "CREATE TABLE query " +
                    "( " +
                        "code TEXT PRIMARY KEY NOT NULL, " +
                        "status INTEGER NOT NULL, " +
                        "type INTEGER NOT NULL, " +
                        "sql TEXT NOT NULL, " +
                        "star BOOLEAN NOT NULL, " +
                        "execution_count INTEGER NOT NULL, " +
                        "last_executed_on TEXT NOT NULL, " +
                        "name TEXT, " +
                        "keywords TEXT " +
                    ");" +

                    "CREATE INDEX idx_query_last_executed_on ON query (last_executed_on);" +
                    "CREATE INDEX idx_query_status ON query (status);" +
                    "CREATE INDEX idx_query_star ON query (star);" +
                    "CREATE INDEX idx_query_name ON query (name);" +

                    "CREATE TABLE stat " +
                    "( " +
                        "code TEXT NOT NULL, " +
                        "status INTEGER NOT NULL, " +
                        "executed_on TEXT NOT NULL, " +
                        "environment TEXT NOT NULL, " +
                        "database TEXT NOT NULL, " +
                        "server_connection TEXT NOT NULL, " +
                        "elapsed INTEGER NOT NULL, " +
                        "row_count INTEGER NOT NULL, " +
                        "records_affected INTEGER NOT NULL " +
                    ");" +

                    "CREATE INDEX idx_stat_code ON stat (code);";

                    await ServerConnection.ExecuteNonQuery(sql);
                }
            }

            public static async Task Save(QueryExecutionContext query, QueryResponseContext response)
            {
                string? statement = NormalizeSql(query.GetSqlStatement());
                if (statement is null)
                {
                    return;
                }

                string sql;
                string code = QueryHistory.GetCode(ComputeHash(statement), query.Server.Type);
                var history = await LoadByCode(code);
                if (history is null)
                {
                    history = QueryHistory.Create(code, statement, query, response);
                    sql = "INSERT INTO query (code, status, type, sql, star, execution_count, last_executed_on) VALUES " +
                    "( " +
                       $"'{history.Code}', " +
                       $"{(int)history.Status}, " +
                       $"{(int)history.Type}, " +
                       $"'{history.Sql.Replace("'", "''")}', " +
                       $"{(history.Star ? 1 : 0)}, " +
                       $"{history.ExecutionCount}, " +
                       $"'{history.LastExecutedOn:yyyy-MM-dd HH:mm:ss}' " +
                    ");";
                }
                else
                {
                    history.UpdateStatistics(query, response);
                    sql = $"UPDATE query SET " +
                             $"status = {(int)history.Status}, " +
                             $"execution_count = {history.ExecutionCount}, " +
                             $"last_executed_on = '{history.LastExecutedOn:yyyy-MM-dd HH:mm:ss}' " +
                          $"WHERE code = '{history.Code}';";
                }

                var stat = history.Stats.Last();
                sql += "INSERT INTO stat (code, status, executed_on, environment, database, server_connection, elapsed, row_count, records_affected) VALUES " +
                "( " +
                   $"'{history.Code}', " +
                   $"{(int)stat.Status}, " +
                   $"'{stat.ExecutedOn:yyyy-MM-dd HH:mm:ss}', " +
                   $"'{stat.Environment}', " +
                   $"'{stat.Database}', " +
                   $"'{stat.ServerConnection}', " +
                   $"{stat.Elapsed}, " +
                   $"{stat.RowCount}, " +
                   $"{stat.RecordsAffected}" +
                ");";

                await ServerConnection.ExecuteNonQuery(sql);
            }

            public static async Task UpdateFavorite(string code, bool star)
                => await ServerConnection.ExecuteNonQuery($"UPDATE query SET star = {(star ? 1 : 0)} WHERE code = '{code}'");

            public static async Task<IEnumerable<QueryHistory>> Load(QueryHistoryQuery query)
                => await QueryList(BuildWhereClause(query));

            private static async Task<QueryHistory?> LoadByCode(string code)
                => (await QueryList($"WHERE q.code = '{code}'")).FirstOrDefault();

            private static async Task<List<QueryHistory>> QueryList(string where)
            {
                var list = new List<QueryHistory>();
                string sql = "SELECT q.code, q.status as status1, type, sql, star, execution_count, last_executed_on, name, keywords, " +
                             "s.status as status2, executed_on, environment, database, server_connection, elapsed, row_count, records_affected " +
                             "FROM query q INNER JOIN stat s ON q.code = s.code " +
                            $"{where} " +
                             "ORDER BY last_executed_on DESC, executed_on ASC";

                using var cnn = ServerConnection.CreateConnection();
                await cnn.OpenAsync();
                using var dbCommand = cnn.CreateCommand();
                dbCommand.CommandText = sql;
                using var dataReader = await dbCommand.ExecuteReaderAsync();

                string previousCode = "";
                QueryHistory history = default!;
                while (await dataReader.ReadAsync())
                {
                    string code = dataReader.GetString(0);
                    if (code != previousCode)
                    {
                        previousCode = code;
                        history = new(code,
                                      status: (QueryResponseStatus)dataReader.GetInt32(1),
                                      type: (DBMS)dataReader.GetInt32(2),
                                      sql: dataReader.GetString(3),
                                      star: dataReader.GetBoolean(4),
                                      executionCount: dataReader.GetInt32(5),
                                      lastExecutedOn: dataReader.GetDateTime(6),
                                      name: dataReader[7] as string ?? null,
                                      keywords: dataReader[8] as string ?? null);

                        list.Add(history);
                    }

                    history.Stats.Add(new(status: (QueryResponseStatus)dataReader.GetInt32(9),
                                          executedOn: dataReader.GetDateTime(10),
                                          environment: dataReader.GetString(11),
                                          database: dataReader.GetString(12),
                                          serverConnection: dataReader.GetString(13),
                                          elapsed: dataReader.GetInt32(14),
                                          rowCount: dataReader.GetInt32(15),
                                          recordsAffected: dataReader.GetInt32(16)));
                }

                return list;
            }

            private static string? NormalizeSql(string? sql) => sql?.TrimStart(CharsToTrimStart)?.TrimEnd(CharsToTrimEnd);

            private static string ComputeHash(string str)
            {
                using var sha256Hash = SHA256.Create();
                var bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(str.ToLowerInvariant()));
                var builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                return builder.ToString();
            }

            private static string BuildWhereClause(QueryHistoryQuery query)
            {
                string sql = "";
                if (query.Sql != null)
                {
                    sql += $" {WhereOrAnd()} sql LIKE '%{query.Sql}%' ";
                }
                if (query.ShowErrors)
                {
                    sql += $" {WhereOrAnd()} q.status = {(int)QueryResponseStatus.Failed} ";
                }
                else
                {
                    sql += $" {WhereOrAnd()} q.status IN ({(int)QueryResponseStatus.Succeeded}, {(int)QueryResponseStatus.Canceled}) ";
                }
                if (query.ShowFavorites)
                {
                    sql += $" {WhereOrAnd()} star = 1 ";
                }

                return sql;

                string WhereOrAnd() => string.IsNullOrEmpty(sql) ? " WHERE " : " AND ";
            }
        }
    }
}
