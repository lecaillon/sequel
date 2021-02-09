using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Sequel.Models;
using static Sequel.Helper;

namespace Sequel.Core
{
    public static class QueryHistoryManager
    {
        public const string TopicSeparator = ";";
        private static readonly char[] CharsToTrimStart = { '\r', '\n' };
        private static readonly char[] CharsToTrimEnd = { '\r', '\n', '\t', ' ' };
        private static readonly ServerConnection ServerConnection = new ServerConnection
        {
            Name = "QueryHistory Sqlite database connection",
            Type = DBMS.SQLite,
            ConnectionString = $@"Data Source={Path.Combine(Program.RootDirectory, typeof(QueryHistory).Name.ToLower() + ".db")};"
        };

        public static async Task Optimize()
        {
            await IgnoreErrorsAsync(() => ServerConnection.ExecuteNonQuery("PRAGMA optimize"));
        }

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
                    "last_environment TEXT NOT NULL, " +
                    "last_database TEXT NOT NULL, " +
                    "name TEXT, " +
                    "topics TEXT " +
                ");" +

                "CREATE INDEX idx_query_status_name ON query (status, name);" +
                "CREATE INDEX idx_query_status_star ON query (status, star, last_database);" +
                "CREATE INDEX idx_query_status_database ON query (status, last_database, last_environment);" +
                "CREATE INDEX idx_query_status_type ON query (status, type, last_environment);" +

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

                "CREATE INDEX idx_stat_code ON stat (code);" +

                "CREATE TABLE topic " +
                "( " +
                    "name TEXT PRIMARY KEY NOT NULL " +
                ");";

                await ServerConnection.ExecuteNonQuery(sql);
            }

            _ = RefreshTopics();
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
                sql = "INSERT INTO query (code, status, type, sql, star, execution_count, last_executed_on, last_environment, last_database) VALUES " +
                "( " +
                   $"'{history.Code}', " +
                   $"{(int)history.Status}, " +
                   $"{(int)history.Type}, " +
                   $"'{history.Sql.Replace("'", "''")}', " +
                   $"{(history.Star ? 1 : 0)}, " +
                   $"{history.ExecutionCount}, " +
                   $"'{history.LastExecutedOn:yyyy-MM-dd HH:mm:ss}', " +
                   $"'{history.Stats.Last().Environment}', " +
                   $"'{history.Stats.Last().Database}' " +
                ");";
            }
            else
            {
                history.UpdateStatistics(query, response);
                sql = $"UPDATE query SET " +
                         $"status = {(int)history.Status}, " +
                         $"execution_count = {history.ExecutionCount}, " +
                         $"last_executed_on = '{history.LastExecutedOn:yyyy-MM-dd HH:mm:ss}', " +
                         $"last_environment = '{history.Stats.Last().Environment}', " +
                         $"last_database = '{history.Stats.Last().Database}' " +
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

        public static async Task UpdateName(string code, string? name)
            => await ServerConnection.ExecuteNonQuery($"UPDATE query SET name = '{name ?? ""}' WHERE code = '{code}'");

        public static async Task<bool> UpdateTopics(string code, List<string> topics)
        {
            topics.RemoveAll(x => string.IsNullOrWhiteSpace(x));
            string separator = topics.Any() ? TopicSeparator : "";
            await ServerConnection.ExecuteNonQuery($"UPDATE query SET topics = '{separator}{string.Join(separator, topics)}{separator}' WHERE code = '{code}'");

            int newTopicCount = 0;
            foreach (var topic in topics)
            {
                string sql = $"INSERT INTO topic (name) SELECT '{topic}' WHERE NOT EXISTS (SELECT name FROM topic WHERE name = '{topic}')";
                newTopicCount += await IgnoreErrorsAsync(() => ServerConnection.ExecuteNonQuery(sql), 0);
            }

            return newTopicCount > 0;
        }

        private static async Task RefreshTopics()
        {
            await IgnoreErrorsAsync(async () =>
            {
                var topics = new HashSet<string>();
                await ServerConnection.ExecuteNonQuery("DELETE FROM topic");
                foreach (var topic in await ServerConnection.QueryStringList("SELECT DISTINCT topics FROM query WHERE topics IS NOT NULL AND topics != ''"))
                {
                    topics.UnionWith(topic.Split(TopicSeparator, StringSplitOptions.RemoveEmptyEntries));
                }
                if (topics.IsNullOrEmpty())
                {
                    return;
                }

                string sql = "";
                foreach (var topic in topics)
                {
                    sql += $"INSERT INTO topic (name) VALUES ('{topic}');";
                }

                await ServerConnection.ExecuteNonQuery(sql);
            });
        }

        public static async Task Delete(string code)
        {
            await ServerConnection.ExecuteNonQuery($"DELETE FROM query WHERE code = '{code}'");
            await ServerConnection.ExecuteNonQuery($"DELETE FROM stat WHERE code = '{code}'");
        }

        public static async Task<IEnumerable<QueryHistory>> Search(QueryHistoryQuery query, List<QueryHistoryTerm> terms)
        {
            Check.NotNull(query, nameof(query));
            Check.NotNull(terms, nameof(terms));

            string where = "";
            if (query.ShowErrors)
            { // status
                where += $" {WhereOrAnd()} q.status = {(int)QueryResponseStatus.Failed} ";
            }
            else
            {
                where += $" {WhereOrAnd()} q.status IN ({(int)QueryResponseStatus.Succeeded}, {(int)QueryResponseStatus.Canceled}) ";
            }
            if (query.ShowNamedQueries)
            { // name
                where += $" {WhereOrAnd()} name IS NOT NULL ";
            }
            if (query.Dbms is not null)
            { // type
                where += $" {WhereOrAnd()} type = {(int)query.Dbms} ";
            }
            foreach (var queryTerm in query.Terms?.Split(",") ?? Enumerable.Empty<string>())
            {
                where += $" {WhereOrAnd()} (";

                bool found = false;
                foreach (var term in terms.Where(x => x.Name == queryTerm))
                {
                    if (term.Kind == QueryHistoryTermKind.Environment)
                    { // last_environment
                        where += $"{(found ? "OR" : "")} last_environment = '{queryTerm}' ";
                        found = true;
                    }
                    if (term.Kind == QueryHistoryTermKind.Database)
                    { // last_database
                        where += $"{(found ? "OR" : "")} last_database = '{queryTerm}' ";
                        found = true;
                    }
                    if (term.Kind == QueryHistoryTermKind.QueryName)
                    { // name
                        where += $"{(found ? "OR" : "")} name = '{queryTerm}' ";
                        found = true;
                    }
                    if (term.Kind == QueryHistoryTermKind.Topic)
                    { // topics
                        where += $"{(found ? "OR" : "")} topics LIKE '%{TopicSeparator}{queryTerm}{TopicSeparator}%' ";
                        found = true;
                    }
                }
                if (!found)
                { // sql
                    where += $" sql LIKE '%{queryTerm}%' ";
                }

                where += $") ";
            }
            if (query.ShowFavorites)
            { // star
                where += $" {WhereOrAnd()} star = 1 ";
            }

            return await QueryList(where);

            string WhereOrAnd() => string.IsNullOrEmpty(where) ? " WHERE " : " AND ";
        }

        public static async Task<List<QueryHistoryTerm>> LoadTerms()
        {
            string sql = $"SELECT DISTINCT {(int)QueryHistoryTermKind.Environment} as kind, last_environment as name, '' as icon FROM query " +
                          "UNION " +
                         $"SELECT DISTINCT {(int)QueryHistoryTermKind.Database} as kind, last_database as name, 'mdi-database' as icon FROM query " +
                          "UNION " +
                         $"SELECT DISTINCT {(int)QueryHistoryTermKind.QueryName} as kind, name, 'mdi-account' as icon FROM query WHERE name IS NOT NULL " +
                          "UNION " +
                         $"SELECT {(int)QueryHistoryTermKind.Topic} as kind, name, 'mdi-shape' as icon FROM topic " +
                         $"ORDER BY kind, name";

            var terms = await ServerConnection.QueryList(sql, r => new QueryHistoryTerm(
                Kind: (QueryHistoryTermKind)r.GetInt32(0),
                Header: null,
                Name: r.GetString(1),
                Icon: r.GetString(2),
                Divider: false));

            var list = new List<QueryHistoryTerm>();
            QueryHistoryTermKind? currentTermKind = null;
            foreach (var term in terms)
            {
                if (term.Kind != currentTermKind)
                {
                    if (currentTermKind != null)
                    {
                        list.Add(new(QueryHistoryTermKind.Divider, Name: null, Header: null, Icon: null, Divider: true));
                    }
                    currentTermKind = term.Kind;
                }
                list.Add(term);
            }

            return list;
        }

        public static async Task<List<QueryHistoryTerm>> LoadTopics()
        {
            var topics = await ServerConnection.QueryStringList("SELECT name FROM topic ORDER BY name");
            var terms = new List<QueryHistoryTerm> { new QueryHistoryTerm(QueryHistoryTermKind.Topic, Name: null, Header: "Topics", Icon: null, Divider: false) };
            terms.AddRange(topics.Select(x => new QueryHistoryTerm(QueryHistoryTermKind.Topic, Name: x, Header: null, Icon: "mdi-shape", Divider: false)));

            return terms;
        }

        private static async Task<QueryHistory?> LoadByCode(string code)
            => (await QueryList($"WHERE q.code = '{code}'")).FirstOrDefault();

        private static async Task<List<QueryHistory>> QueryList(string where)
        {
            var list = new List<QueryHistory>();
            string sql = "SELECT q.code, q.status as status1, type, sql, star, execution_count, last_executed_on, last_environment, last_database, name, topics, " +
                         "s.status as status2, executed_on, environment, database, server_connection, elapsed, row_count, records_affected " +
                         "FROM query q INNER JOIN stat s ON q.code = s.code " +
                        $"{where} " +
                         "ORDER BY last_executed_on DESC, executed_on DESC";

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
                                  lastEnvironment: dataReader.GetString(7),
                                  lastDatabase: dataReader.GetString(8),
                                  name: dataReader[9] as string ?? null,
                                  topics: dataReader[10] as string ?? null);

                    list.Add(history);
                }

                history.Stats.Add(new(status: (QueryResponseStatus)dataReader.GetInt32(11),
                                      executedOn: dataReader.GetDateTime(12),
                                      environment: dataReader.GetString(13),
                                      database: dataReader.GetString(14),
                                      serverConnection: dataReader.GetString(15),
                                      elapsed: dataReader.GetInt32(16),
                                      rowCount: dataReader.GetInt32(17),
                                      recordsAffected: dataReader.GetInt32(18)));
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
    }
}
