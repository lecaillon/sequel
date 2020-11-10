using System.Collections.Generic;
using System.Linq;
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

        public override async Task<IEnumerable<string>> LoadSchemas(string? database)
        {
            return await _server.QueryStringList(database,
                "SELECT schema_name " +
                "FROM information_schema.schemata " +
                "WHERE schema_name NOT LIKE 'pg_%' " +
                "AND schema_name <> 'information_schema' " +
                "ORDER BY schema_name");
        }

        public override async Task<IEnumerable<string>> LoadTables(string? database, string? schema)
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

        public override async Task<IEnumerable<TreeViewNode>> LoadTreeViewNodes(string? database, TreeViewNode? parent)
        {
            Check.NotNull(database, nameof(database));

            return parent?.Type switch
            {
                null => LoadDatabaseRootNode(database),

                Schemas => await LoadSchemaNodesAsync(database, parent),
                Tables => await LoadTableNodesAsync(database, parent),
                Functions => await LoadFunctionNodesAsync(database, parent),
                Columns => await LoadColumnNodes(database, parent),

                Schema => LoadSchemaGroupLabels(parent),
                Table => LoadTableGroupLabels(parent),

                _ => new List<TreeViewNode>()
            };
        }

        protected override Task<Dictionary<string, string>> GetPlaceholders(TreeViewNode node)
        {
            return Task.FromResult(new Dictionary<string, string>
            {
                { "${schema}", GetSchema(node) },
                { "${table}", GetTable(node) },
            });
        }

        private async Task<long> GetVersion() => await _server.QueryForLong("SHOW server_version_num");

        private IEnumerable<TreeViewNode> LoadDatabaseRootNode(string database)
        {
            var rootNode = new TreeViewNode(database, Database, parent: null, "mdi-database", "amber darken-1");
            rootNode.Children.Add(new TreeViewNode(Schemas.ToString(), Schemas, rootNode, "mdi-hexagon-multiple-outline", "cyan"));

            return new List<TreeViewNode> { rootNode };
        }

        private async Task<IEnumerable<TreeViewNode>> LoadSchemaNodesAsync(string database, TreeViewNode parent)
        {
            return (await LoadSchemas(database))
                .Select(schema => new TreeViewNode(schema, Schema, parent, "mdi-hexagon-multiple-outline", "cyan"));
        }

        private IEnumerable<TreeViewNode> LoadSchemaGroupLabels(TreeViewNode parent)
        {
            return new List<TreeViewNode>
            {
                new TreeViewNode(Tables.ToString(), Tables, parent, "mdi-table", "blue"),
                new TreeViewNode(Functions.ToString(), Functions, parent, "mdi-function", "teal")
            };
        }

        private async Task<IEnumerable<TreeViewNode>> LoadTableNodesAsync(string database, TreeViewNode parent)
        {
            return (await LoadTables(database, GetSchema(parent)))
                .Select(table => new TreeViewNode(table, Table, parent, "mdi-table", "blue"));
        }

        private IEnumerable<TreeViewNode> LoadTableGroupLabels(TreeViewNode parent)
        {
            return new List<TreeViewNode>
            {
                new TreeViewNode(Columns.ToString(), Columns, parent, "mdi-table-column", "deep-purple"),
            };
        }

        public override async Task<IEnumerable<string>> LoadColumns(string? database, string? schema, string table)
        {
            Check.NotNull(schema, nameof(schema));

            return await _server.QueryStringList(database,
                "SELECT column_name " +
                "FROM information_schema.columns " +
               $"WHERE table_schema = '{schema}' " +
               $"AND table_name = '{table}' ");
        }

        private async Task<IEnumerable<TreeViewNode>> LoadColumnNodes(string database, TreeViewNode parent)
        {
            string sql = 
                "SELECT column_name, data_type " +
                "FROM information_schema.columns " +
               $"WHERE table_schema = '{GetSchema(parent)}' " +
               $"AND table_name = '{GetTable(parent)}'";

            var list = await _server.QueryList(database, sql, reader => new { Name = reader.GetString(0), Type = reader.GetString(1) });

            return (await _server.QueryList(database, sql, reader => new { Name = reader.GetString(0), Type = reader.GetString(1) }))
                .Select(x => new TreeViewNode(x.Name, Column, parent, "mdi-table-column", "deep-purple",
                                                    details: new Dictionary<string, object> { ["type"] = x.Type }));

        }

        private async Task<IEnumerable<(string Name, string Args)>> LoadFunctionsAsync(string database, string schema)
        {
            string sql;

            if (await GetVersion() < 11000)
            {
                sql = "SELECT proname, oidvectortypes(proargtypes) AS args " +
                      "FROM pg_proc INNER JOIN pg_namespace ns ON (pg_proc.pronamespace = ns.oid) " +
                      "LEFT JOIN pg_depend dep ON dep.objid = pg_proc.oid AND dep.deptype = 'e' " +
                     $"WHERE pg_proc.proisagg = false " +
                     $"AND ns.nspname = '{schema}' " +
                     $"AND dep.objid IS NULL";
            }
            else
            {
                sql = "SELECT proname, oidvectortypes(proargtypes) AS args " +
                      "FROM pg_proc INNER JOIN pg_namespace ns ON (pg_proc.pronamespace = ns.oid) " +
                      "LEFT JOIN pg_depend dep ON dep.objid = pg_proc.oid AND dep.deptype = 'e' " +
                     $"WHERE ns.nspname = '{schema}' " +
                     $"AND dep.objid IS NULL " +
                     $"AND pg_proc.prokind = 'f'";
            }

            return await _server.QueryList(database, sql, reader => (Name: reader.GetString(0), Args: reader.GetString(1)));
        }

        private async Task<IEnumerable<TreeViewNode>> LoadFunctionNodesAsync(string database, TreeViewNode parent)
        {
            return (await LoadFunctionsAsync(database, GetSchema(parent)))
                .Select(x => new TreeViewNode(x.Name, Function, parent, "mdi-function", "teal",
                                                    details: new Dictionary<string, object> { ["args"] = x.Args }));
        }

        private static string GetSchema(TreeViewNode node) => node.Id.Split(TreeViewNode.PathSeparator)[2];

        private static string GetTable(TreeViewNode node) => node.Id.Split(TreeViewNode.PathSeparator)[4];

        public override async Task<string?> GetCurrentSchema(string? database)
            => CleanSchemaName(await _server.QueryForString(database, "SHOW search_path"));

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
