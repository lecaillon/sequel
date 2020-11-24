using System.Collections.Generic;
using System.Threading.Tasks;
using Sequel.Core;
using Sequel.Models;
using static Sequel.TreeViewNodeType;

namespace Sequel.Databases
{
    public class PostgreSQL : DatabaseSystem
    {
        internal static readonly List<TreeViewMenuItem> TreeViewMenuItems = new List<TreeViewMenuItem>
        {
            new TreeViewMenuItem("All rows", "SELECT * FROM ${schema}.${table}", "mdi-database-search", 1000, new[] { DBMS.PostgreSQL }, new[] { Table }),
            new TreeViewMenuItem("First 100 rows", "SELECT * FROM ${schema}.${table} LIMIT 100", "mdi-database-search", 1010, new[] { DBMS.PostgreSQL }, new[] { Table }),
        };

        private readonly ServerConnection _server;

        public PostgreSQL(ServerConnection server)
        {
            _server = Check.NotNull(server, nameof(server));
        }

        public override DBMS Type => DBMS.PostgreSQL;

        public override async Task<IEnumerable<string>> LoadDatabases()
        {
            return await _server.QueryStringList(
                "SELECT datname " +
                "FROM pg_database " +
                "WHERE datistemplate = false " +
                "ORDER BY datname");
        }

        protected override async Task<string?> GetCurrentSchema(string database)
            => CleanSchemaName(await _server.QueryForString(database, "SHOW search_path"));

        protected override async Task<IEnumerable<string>> LoadSchemas(string database)
        {
            return await _server.QueryStringList(database,
                "SELECT schema_name " +
                "FROM information_schema.schemata " +
                "WHERE schema_name NOT LIKE 'pg_%' " +
                "AND schema_name <> 'information_schema' " +
                "ORDER BY schema_name");
        }

        protected override async Task<IEnumerable<string>> LoadTables(string database, string? schema)
        {
            Check.NotNullOrEmpty(schema, nameof(schema));

            return await _server.QueryStringList(database,
                "SELECT t.table_name " +
                "FROM information_schema.tables t " +
                "LEFT JOIN pg_depend dep ON dep.objid = (quote_ident(t.table_schema)||'.'||quote_ident(t.table_name))::regclass::oid AND dep.deptype = 'e' " +
               $"WHERE table_schema = '{schema}' " +
                "AND table_type='BASE TABLE' " +
                "AND dep.objid IS NULL " +
                "AND NOT (SELECT EXISTS (SELECT inhrelid FROM pg_catalog.pg_inherits WHERE inhrelid = (quote_ident(t.table_schema)||'.'||quote_ident(t.table_name))::regclass::oid)) " +
                "ORDER BY t.table_name");
        }

        protected override async Task<IEnumerable<string>> LoadViews(string database, string? schema)
        {
            Check.NotNullOrEmpty(schema, nameof(schema));

            return await _server.QueryStringList(database,
                "SELECT relname " +
                "FROM pg_catalog.pg_class c " +
                "JOIN pg_namespace n ON n.oid = c.relnamespace " +
                "LEFT JOIN pg_depend dep ON dep.objid = c.oid AND dep.deptype = 'e' " +
               $"WHERE c.relkind = 'v' AND  n.nspname = '{schema}' AND dep.objid IS NULL");
        }

        protected override async Task<IEnumerable<string>> LoadFunctions(string database, string? schema)
        {
            Check.NotNullOrEmpty(schema, nameof(schema));

            string sql;

            if (await GetVersion() < 11000)
            {
                sql = "SELECT pg_proc.proname " +
                      "FROM pg_proc INNER JOIN pg_namespace ns ON (pg_proc.pronamespace = ns.oid) " +
                      "LEFT JOIN pg_depend dep ON dep.objid = pg_proc.oid AND dep.deptype = 'e' " +
                      "WHERE pg_proc.proisagg = false " +
                     $"AND ns.nspname = '{schema}' " +
                      "AND dep.objid IS NULL " +
                      "ORDER BY pg_proc.proname";
            }
            else
            {
                sql = "SELECT pg_proc.proname " +
                      "FROM pg_proc INNER JOIN pg_namespace ns ON (pg_proc.pronamespace = ns.oid) " +
                      "LEFT JOIN pg_depend dep ON dep.objid = pg_proc.oid AND dep.deptype = 'e' " +
                     $"WHERE ns.nspname = '{schema}' " +
                      "AND dep.objid IS NULL " +
                      "AND pg_proc.prokind = 'f' " +
                      "ORDER BY pg_proc.proname";
            }

            return await _server.QueryStringList(database, sql);
        }

        protected override async Task<IEnumerable<string>> LoadProcedures(string database, string? schema)
        {
            Check.NotNull(schema, nameof(schema));

            return await _server.QueryStringList(database,
                "SELECT pg_proc.proname " +
                "FROM pg_proc INNER JOIN pg_namespace ns ON (pg_proc.pronamespace = ns.oid) " +
                "LEFT JOIN pg_depend dep ON dep.objid = pg_proc.oid AND dep.deptype = 'e' " +
                $"WHERE ns.nspname = '{schema}' " +
                "AND dep.objid IS NULL " +
                "AND pg_proc.prokind = 'p' " +
                "ORDER BY pg_proc.proname");
        }

        protected override async Task<IEnumerable<string>> LoadSequences(string database, string? schema)
        {
            Check.NotNull(schema, nameof(schema));

            return await _server.QueryStringList(database,
                "SELECT sequence_name " +
                "FROM information_schema.sequences " +
               $"WHERE sequence_schema = '{schema}' " +
                "ORDER BY sequence_name");
        }

        protected override async Task<IEnumerable<string>> LoadTableColumns(string database, string? schema, string table)
        {
            Check.NotNull(schema, nameof(schema));

            return await _server.QueryStringList(database,
                "SELECT column_name " +
                "FROM information_schema.columns " +
               $"WHERE table_schema = '{schema}' " +
               $"AND table_name = '{table}' " +
               $"ORDER BY column_name");
        }

        protected override async Task<IEnumerable<string>> LoadIndexes(string database, string? schema, string table)
        {
            Check.NotNull(schema, nameof(schema));

            return await _server.QueryStringList(database,
                "SELECT indexname " +
                "FROM pg_indexes " +
               $"WHERE schemaname = '{schema}' " +
               $"AND tablename = '{table}' " +
               $"ORDER BY indexname");
        }

        protected override async Task<IEnumerable<string>> LoadViewColumns(string database, string? schema, string view)
        {
            Check.NotNull(schema, nameof(schema));

            return await _server.QueryStringList(database,
                "SELECT attname" + //, format_type(atttypid, atttypmod) AS data_type
                "FROM pg_attribute" +
               $"WHERE attrelid = '{schema}.{view}'::regclass" +
                "ORDER BY attnum");
        }

        private async Task<long> GetVersion() => await _server.QueryForLong("SHOW server_version_num");

        private static string CleanSchemaName(string? schema)
        {
            if (schema is null)
            {
                return string.Empty;
            }

            string newSchema = schema.Replace("\"", "").Replace("$user", "").Trim();
            if (newSchema.StartsWith(","))
            {
                newSchema = newSchema[1..];
            }

            if (newSchema.Contains(","))
            {
                newSchema = newSchema.Substring(0, newSchema.IndexOf(","));
            }

            return newSchema.Trim();
        }
    }
}
