using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sequel.Core;
using Sequel.Models;
using static Sequel.DatabaseObjectType;

namespace Sequel.Databases
{
    public class PostgreSQL : IDatabaseSystem
    {
        private readonly ServerConnection _server;

        public PostgreSQL(ServerConnection server)
        {
            _server = Check.NotNull(server, nameof(server));
        }

        public DBMS Type => _server.Type;

        public async Task<long> GetVersion() => await _server.QueryForLongAsync("SHOW server_version_num");

        public async Task<IEnumerable<string>> LoadDatabasesAsync()
        {
            return await _server.QueryStringListAsync(
                "SELECT datname " +
                "FROM pg_database " +
                "WHERE datistemplate = false " +
                "ORDER BY datname");
        }

        public async Task<IEnumerable<DatabaseObjectNode>> LoadDatabaseObjectNodesAsync(string database, DatabaseObjectNode? parent)
        {
            return (parent) switch
            {
                null => LoadDatabaseRootNode(database),
                { Type: GroupLabel, Name: Label.Schemas } => await LoadSchemaNodesAsync(database, parent),
                { Type: GroupLabel, Name: Label.Tables } => await LoadTableNodesAsync(database, parent),
                { Type: GroupLabel, Name: Label.Functions } => await LoadFunctionNodesAsync(database, parent),
                { Type: GroupLabel, Name: Label.Columns } => await LoadColumnNodes(database, parent),
                { Type: Schema } => LoadSchemaGroupLabels(parent),
                { Type: Table } => LoadTableGroupLabels(parent),
                _ => new List<DatabaseObjectNode>()
            };
        }

        public async Task<IEnumerable<CompletionItem>> LoadIntellisenseAsync(string database)
        {
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

        private IEnumerable<DatabaseObjectNode> LoadDatabaseRootNode(string database)
        {
            var rootNode = new DatabaseObjectNode(database, Database, parent: null, "mdi-database", "amber darken-1");
            rootNode.Children.Add(new DatabaseObjectNode(Label.Schemas, GroupLabel, rootNode, "mdi-hexagon-multiple-outline", "cyan"));

            return new List<DatabaseObjectNode> { rootNode };
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

        private async Task<IEnumerable<DatabaseObjectNode>> LoadSchemaNodesAsync(string database, DatabaseObjectNode parent)
        {
            return (await LoadSchemasAsync(database))
                .Select(schema => new DatabaseObjectNode(schema, Schema, parent, "mdi-hexagon-multiple-outline", "cyan"));
        }

        private IEnumerable<DatabaseObjectNode> LoadSchemaGroupLabels(DatabaseObjectNode parent)
        {
            return new List<DatabaseObjectNode>
            {
                new DatabaseObjectNode(Label.Tables, GroupLabel, parent, "mdi-table", "blue"),
                new DatabaseObjectNode(Label.Functions, GroupLabel, parent, "mdi-function", "teal")
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

        private async Task<IEnumerable<DatabaseObjectNode>> LoadTableNodesAsync(string database, DatabaseObjectNode parent)
        {
            return (await LoadTablesAsync(database, GetSchema(parent)))
                .Select(table => new DatabaseObjectNode(table, Table, parent, "mdi-table", "blue"));
        }

        private IEnumerable<DatabaseObjectNode> LoadTableGroupLabels(DatabaseObjectNode parent)
        {
            return new List<DatabaseObjectNode>
            {
                new DatabaseObjectNode(Label.Columns, GroupLabel, parent, "mdi-table-column", "deep-purple"),
            };
        }

        private async Task<IEnumerable<string>> LoadColumns(string database, string schema)
        {
            return await _server.QueryStringListAsync(database,
                "SELECT distinct column_name, data_type " +
                "FROM information_schema.columns " +
               $"WHERE table_schema = '{schema}'");
        }

        private async Task<IEnumerable<DatabaseObjectNode>> LoadColumnNodes(string database, DatabaseObjectNode parent)
        {
            string sql = 
                "SELECT column_name, data_type " +
                "FROM information_schema.columns " +
               $"WHERE table_schema = '{GetSchema(parent)}' " +
               $"AND table_name = '{GetTable(parent)}'";

            return (await _server.QueryListAsync(database, sql, reader => new { Name = reader.GetString(0), Type = reader.GetString(1) }))
                .Select(x => new DatabaseObjectNode(x.Name, Column, parent, "mdi-table-column", "deep-purple",
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

        private async Task<IEnumerable<DatabaseObjectNode>> LoadFunctionNodesAsync(string database, DatabaseObjectNode parent)
        {
            return (await LoadFunctionsAsync(database, GetSchema(parent)))
                .Select(x => new DatabaseObjectNode(x.Name, Function, parent, "mdi-function", "teal",
                                                    details: new Dictionary<string, object> { ["args"] = x.Args }));
        }

        private static string GetSchema(DatabaseObjectNode node) => node.Id.Split(DatabaseObjectNode.PathSeparator)[2];

        private static string GetTable(DatabaseObjectNode node) => node.Id.Split(DatabaseObjectNode.PathSeparator)[4];

        private static class Label
        {
            public const string Schemas = "Schemas";
            public const string Tables = "Tables";
            public const string Functions = "Functions";
            public const string Columns = "Columns";
        }
    }
}
