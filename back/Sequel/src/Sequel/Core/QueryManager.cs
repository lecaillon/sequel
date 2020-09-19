using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
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

        public static async Task<QueryResponseContext> ExecuteQueryAsync(QueryExecutionContext context)
        {
            string queryId = Check.NotNull(context.Id, nameof(context.Id));
            var cancellationToken = CreateToken(queryId);

            try
            {
                return await context.ExecuteQueryAsync(cancellationToken);
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

        private static async Task<QueryResponseContext> ExecuteQueryAsync(this QueryExecutionContext context, CancellationToken cancellationToken)
        {
            return await context.Server.ExecuteAsync(context.Database, context.Sql!, async (dbCommand, ct) =>
            {
                var response = new QueryResponseContext(context.Id!);
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
                    await IgnoreErrorsAsync(() => History.SaveAsync(context, response));
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
            private const string SelectAllClause = "SELECT id, type, server_connection, sql, hash, executed_on, status, elapsed, row_count, records_affected, execution_count, star FROM [data] ";
            private const string OrderByClause = " ORDER BY executed_on DESC ";
            private static readonly char[] CharsToTrimStart = { '\r', '\n' };
            private static readonly char[] CharsToTrimEnd = { '\r', '\n', '\t', ' ' };
            private static readonly Func<IDataReader, QueryHistory> Map = r =>
            {
                return new QueryHistory
                {
                    Id = r.GetInt32(0),
                    Type = (DBMS)r.GetInt32(1),
                    ServerConnection = r.GetString(2),
                    Sql = r.GetString(3),
                    Hash = r.GetString(4),
                    ExecutedOn = r.GetDateTime(5),
                    Status = (QueryResponseStatus)r.GetInt32(6),
                    Elapsed = r.GetInt32(7),
                    RowCount = r.GetInt32(8),
                    RecordsAffected = r.GetInt32(9),
                    ExecutionCount = r.GetInt32(10),
                    Star = r.GetBoolean(11)
                };
            };
            private static readonly ServerConnection ServerConnection = new ServerConnection
            {
                Name = "QueryHistory Sqlite database connection",
                Type = DBMS.SQLite,
                ConnectionString = $@"Data Source={Path.Combine(Program.RootDirectory, typeof(QueryHistory).Name.ToLower() + ".db")};"
            };

            public static async Task ConfigureAsync()
            {
                if (await ServerConnection.QueryForLongAsync("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND tbl_name = 'data'") == 0)
                {
                    string sql = "CREATE TABLE [data] " +
                    "( " +
                        "id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, " +
                        "type INTEGER NOT NULL, " +
                        "server_connection TEXT NOT NULL, " +
                        "sql TEXT NOT NULL, " +
                        "hash TEXT NOT NULL, " +
                        "executed_on TEXT NOT NULL, " +
                        "status INTEGER NOT NULL, " +
                        "elapsed INTEGER NOT NULL, " +
                        "row_count INTEGER NOT NULL, " +
                        "records_affected INTEGER NOT NULL, " +
                        "execution_count INTEGER NOT NULL, " +
                        "star BOOLEAN NOT NULL " +
                    ");" +
                    "CREATE UNIQUE INDEX idx_data_hash ON data (hash);";

                    await ServerConnection.ExecuteNonQueryAsync(sql);
                }
            }

            public static async Task SaveAsync(QueryExecutionContext query, QueryResponseContext response)
            {
                string? statement = NormalizeSql(query.Sql);
                if (statement is null)
                {
                    return;
                }

                string sql;
                string hash = ComputeHash(statement);
                var history = await LoadByHashAsync(hash);
                if (history is null)
                {
                    history = QueryHistory.Create(statement, hash, query, response);
                    sql = "INSERT INTO [data] (type, server_connection, sql, hash, executed_on, status, elapsed, row_count, records_affected, execution_count, star) VALUES " +
                    "( " +
                       $"{(int)history.Type}, " +
                       $"'{history.ServerConnection}', " +
                       $"'{history.Sql.Replace("'", "''")}', " +
                       $"'{history.Hash}', " +
                       $"'{history.ExecutedOn:yyyy-MM-dd HH:mm:ss}', " +
                       $"{(int)history.Status}, " +
                       $"{history.Elapsed}, " +
                       $"{history.RowCount}, " +
                       $"{history.RecordsAffected}, " +
                       $"{history.ExecutionCount}, " +
                       $"{(history.Star ? 1 : 0)}" +
                    ");";
                }
                else
                {
                    history.UpdateStatistics(query, response);
                    sql = "UPDATE [data] " +
                         $"SET server_connection = '{history.ServerConnection}', " +
                             $"executed_on = '{history.ExecutedOn:yyyy-MM-dd HH:mm:ss}', " +
                             $"status = {(int)history.Status}, " +
                             $"elapsed = {history.Elapsed}, " +
                             $"row_count = {history.RowCount}, " +
                             $"records_affected = {history.RecordsAffected}, " +
                             $"execution_count = {history.ExecutionCount} " +
                         $"WHERE id = {history.Id};";
                }

                await ServerConnection.ExecuteNonQueryAsync(sql);
            }

            public static async Task UpdateFavorite(int id, bool star)
                => await ServerConnection.ExecuteNonQueryAsync($"UPDATE [data] SET star = {(star ? 1 : 0)} WHERE id = {id}");

            public static async Task<IEnumerable<QueryHistory>> Load(QueryHistoryQuery query)
                => await ServerConnection.QueryListAsync(SelectAllClause + query.BuildWhereClause() + OrderByClause, Map);

            private static async Task<QueryHistory?> LoadByHashAsync(string hash) 
                => await ServerConnection.QueryAsync(SelectAllClause + $"WHERE hash = '{hash}'", Map);

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
        }

        private static string BuildWhereClause(this QueryHistoryQuery query)
        {
            string sql = "";
            if (query.Sql != null)
            {
                sql += $" {WhereOrAnd()} sql LIKE '%{query.Sql}%' ";
            }
            if (!query.DisplayErrors)
            {
                sql += $" {WhereOrAnd()} status = {(int)QueryResponseStatus.Succeeded} ";
            }

            return sql;

            string WhereOrAnd() => string.IsNullOrEmpty(sql) ? " WHERE " : " AND ";
        }
    }
}
