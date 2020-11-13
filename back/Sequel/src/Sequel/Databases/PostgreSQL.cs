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

        protected override async Task<string?> GetCurrentSchema(string database)
            => CleanSchemaName(await _server.QueryForString(database, "SHOW search_path"));

        public override async Task<IEnumerable<string>> LoadDatabases()
        {
            return await _server.QueryStringList(
                "SELECT datname " +
                "FROM pg_database " +
                "WHERE datistemplate = false " +
                "ORDER BY datname");
        }

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
                "AND NOT (SELECT EXISTS (SELECT inhrelid FROM pg_catalog.pg_inherits WHERE inhrelid = (quote_ident(t.table_schema)||'.'||quote_ident(t.table_name))::regclass::oid))");
        }

        protected override async Task<IEnumerable<string>> LoadFunctions(string database, string schema)
        {
            string sql;

            if (await GetVersion() < 11000)
            {
                sql = "SELECT pg_proc.proname " +
                      "FROM pg_proc INNER JOIN pg_namespace ns ON (pg_proc.pronamespace = ns.oid) " +
                      "LEFT JOIN pg_depend dep ON dep.objid = pg_proc.oid AND dep.deptype = 'e' " +
                     $"WHERE pg_proc.proisagg = false " +
                     $"AND ns.nspname = '{schema}' " +
                     $"AND dep.objid IS NULL";
            }
            else
            {
                sql = "SELECT pg_proc.proname " +
                      "FROM pg_proc INNER JOIN pg_namespace ns ON (pg_proc.pronamespace = ns.oid) " +
                      "LEFT JOIN pg_depend dep ON dep.objid = pg_proc.oid AND dep.deptype = 'e' " +
                     $"WHERE ns.nspname = '{schema}' " +
                     $"AND dep.objid IS NULL " +
                     $"AND pg_proc.prokind = 'f'";
            }

            return await _server.QueryStringList(database, sql);
        }

        protected override async Task<IEnumerable<string>> LoadColumns(string database, string? schema, string table)
        {
            Check.NotNull(schema, nameof(schema));

            return await _server.QueryStringList(database,
                "SELECT column_name " +
                "FROM information_schema.columns " +
               $"WHERE table_schema = '{schema}' " +
               $"AND table_name = '{table}' ");
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
