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

        public override async Task<IEnumerable<string>> LoadDatabasesAsync()
        {
            return await _server.QueryStringListAsync(
                "SELECT datname " +
                "FROM pg_database " +
                "WHERE datistemplate = false " +
                "ORDER BY datname");
        }

        public override async Task<IEnumerable<TreeViewNode>> LoadTreeViewNodesAsync(string? database, TreeViewNode? parent)
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

        public override async Task<IEnumerable<CompletionItem>> LoadIntellisenseAsync(string? database)
        {
            Check.NotNull(database, nameof(database));

            var items = new List<CompletionItem>();
            var schemas = await LoadSchemasAsync(database);

            items.AddRange(schemas.Select(schema => new CompletionItem(schema, CompletionItemKind.Module)));

            items.AddRange((await Task.WhenAll(schemas.Select(schema => LoadTablesAsync(database, schema))))
                 .SelectMany(tables => tables)
                 .Distinct()
                 .Select(t => new CompletionItem(t, CompletionItemKind.Constant)));
            
            items.AddRange((await Task.WhenAll(schemas.Select(schema => LoadFunctionsAsync(database, schema))))
                 .SelectMany(functions => functions)
                 .Select(f => f.Name)
                 .Distinct()
                 .Select(f => new CompletionItem(f, CompletionItemKind.Function)));

            items.AddRange((await Task.WhenAll(schemas.Select(schema => LoadColumns(database, schema))))
                 .SelectMany(cols => cols)
                 .Distinct()
                 .Select(c => new CompletionItem(c, CompletionItemKind.Field)));

            return items;
        }

        protected override Task<Dictionary<string, string>> GetPlaceholdersAsync(TreeViewNode node)
        {
            return Task.FromResult(new Dictionary<string, string>
            {
                { "${schema}", GetSchema(node) },
                { "${table}", GetTable(node) },
            });
        }

        private async Task<long> GetVersion() => await _server.QueryForLongAsync("SHOW server_version_num");

        private IEnumerable<TreeViewNode> LoadDatabaseRootNode(string database)
        {
            var rootNode = new TreeViewNode(database, Database, parent: null, "mdi-database", "amber darken-1");
            rootNode.Children.Add(new TreeViewNode(Schemas.ToString(), Schemas, rootNode, "mdi-hexagon-multiple-outline", "cyan"));

            return new List<TreeViewNode> { rootNode };
        }

        private async Task<IEnumerable<string>> LoadSchemasAsync(string database)
        {
            return await _server.QueryStringListAsync(database,
                "SELECT schema_name " +
                "FROM information_schema.schemata " +
                "WHERE schema_name NOT LIKE 'pg_%' " +
                "AND schema_name <> 'information_schema' " +
                "ORDER BY schema_name");
        }

        private async Task<IEnumerable<TreeViewNode>> LoadSchemaNodesAsync(string database, TreeViewNode parent)
        {
            return (await LoadSchemasAsync(database))
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

        private async Task<IEnumerable<string>> LoadTablesAsync(string database, string schema)
        {
            return await _server.QueryStringListAsync(database,
                "SELECT t.table_name " +
                "FROM information_schema.tables t " +
                "LEFT JOIN pg_depend dep ON dep.objid = (quote_ident(t.table_schema)||'.'||quote_ident(t.table_name))::regclass::oid AND dep.deptype = 'e' " +
               $"WHERE table_schema = '{schema}' " +
                "AND table_type='BASE TABLE' " +
                "AND dep.objid IS NULL " +
                "AND NOT (SELECT EXISTS (SELECT inhrelid FROM pg_catalog.pg_inherits WHERE inhrelid = (quote_ident(t.table_schema)||'.'||quote_ident(t.table_name))::regclass::oid))");
        }

        private async Task<IEnumerable<TreeViewNode>> LoadTableNodesAsync(string database, TreeViewNode parent)
        {
            return (await LoadTablesAsync(database, GetSchema(parent)))
                .Select(table => new TreeViewNode(table, Table, parent, "mdi-table", "blue"));
        }

        private IEnumerable<TreeViewNode> LoadTableGroupLabels(TreeViewNode parent)
        {
            return new List<TreeViewNode>
            {
                new TreeViewNode(Columns.ToString(), Columns, parent, "mdi-table-column", "deep-purple"),
            };
        }

        private async Task<IEnumerable<string>> LoadColumns(string database, string schema)
        {
            return await _server.QueryStringListAsync(database,
                "SELECT distinct column_name, data_type " +
                "FROM information_schema.columns " +
               $"WHERE table_schema = '{schema}'");
        }

        private async Task<IEnumerable<TreeViewNode>> LoadColumnNodes(string database, TreeViewNode parent)
        {
            string sql = 
                "SELECT column_name, data_type " +
                "FROM information_schema.columns " +
               $"WHERE table_schema = '{GetSchema(parent)}' " +
               $"AND table_name = '{GetTable(parent)}'";

            var list = await _server.QueryListAsync(database, sql, reader => new { Name = reader.GetString(0), Type = reader.GetString(1) });

            return (await _server.QueryListAsync(database, sql, reader => new { Name = reader.GetString(0), Type = reader.GetString(1) }))
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

            return await _server.QueryListAsync(database, sql, reader => (Name: reader.GetString(0), Args: reader.GetString(1)));
        }

        private async Task<IEnumerable<TreeViewNode>> LoadFunctionNodesAsync(string database, TreeViewNode parent)
        {
            return (await LoadFunctionsAsync(database, GetSchema(parent)))
                .Select(x => new TreeViewNode(x.Name, Function, parent, "mdi-function", "teal",
                                                    details: new Dictionary<string, object> { ["args"] = x.Args }));
        }

        private static string GetSchema(TreeViewNode node) => node.Id.Split(TreeViewNode.PathSeparator)[2];

        private static string GetTable(TreeViewNode node) => node.Id.Split(TreeViewNode.PathSeparator)[4];
    }
}
