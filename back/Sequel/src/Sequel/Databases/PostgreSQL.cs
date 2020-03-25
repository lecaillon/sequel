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

        public async Task<long> GetVersion() => await _server.QueryForLong("SHOW server_version_num");

        public async Task<IEnumerable<string>> LoadDatabases()
        {
            return await _server.QueryForListOfString(
                "SELECT datname " +
                "FROM pg_database " +
                "WHERE datistemplate = false " +
                "ORDER BY datname");
        }

        public async Task<IEnumerable<DatabaseObjectNode>> LoadDatabaseObjects(string database, DatabaseObjectNode? parent)
        {
            return (parent) switch
            {
                null => LoadDatabase(database),
                { Type: GroupLabel, Name: Label.Schemas } => await LoadSchemas(database, parent),
                { Type: GroupLabel, Name: Label.Tables } => await LoadTables(database, parent),
                { Type: GroupLabel, Name: Label.Functions } => await LoadFunctions(database, parent),
                { Type: GroupLabel, Name: Label.Columns } => await LoadColumns(database, parent),
                { Type: Schema } => LoadSchemaGroupLabels(parent),
                { Type: Table } => LoadTableGroupLabels(parent),
                _ => new List<DatabaseObjectNode>()
            };
        }

        private IEnumerable<DatabaseObjectNode> LoadDatabase(string database)
        {
            var rootNode = new DatabaseObjectNode(database, Database, parent: null, "mdi-database");
            rootNode.Children.Add(new DatabaseObjectNode(Label.Schemas, GroupLabel, rootNode, "mdi-hexagon-multiple-outline"));

            return new List<DatabaseObjectNode> { rootNode };
        }

        private async Task<IEnumerable<DatabaseObjectNode>> LoadSchemas(string database, DatabaseObjectNode parent)
        {
            var schemas = await _server.QueryForListOfString(database, 
                "SELECT schema_name " +
                "FROM information_schema.schemata " +
                "WHERE schema_name NOT LIKE 'pg_%' " +
                "AND schema_name <> 'information_schema' " +
                "ORDER BY schema_name");

            return schemas.Select(schema => new DatabaseObjectNode(schema, Schema, parent, "mdi-hexagon-multiple-outline"));
        }

        private IEnumerable<DatabaseObjectNode> LoadSchemaGroupLabels(DatabaseObjectNode parent)
        {
            return new List<DatabaseObjectNode>
            {
                new DatabaseObjectNode(Label.Tables, GroupLabel, parent, "mdi-table"),
                new DatabaseObjectNode(Label.Functions, GroupLabel, parent, "mdi-function")
            };
        }

        private async Task<IEnumerable<DatabaseObjectNode>> LoadTables(string database, DatabaseObjectNode parent)
        {
            var tables = await _server.QueryForListOfString(database,
                "SELECT t.table_name " +
                "FROM information_schema.tables t " +
                "LEFT JOIN pg_depend dep ON dep.objid = (quote_ident(t.table_schema)||'.'||quote_ident(t.table_name))::regclass::oid AND dep.deptype = 'e' " +
               $"WHERE table_schema = '{GetSchema(parent)}' " +
                "AND table_type='BASE TABLE' " +
                "AND dep.objid IS NULL " +
                "AND NOT (SELECT EXISTS (SELECT inhrelid FROM pg_catalog.pg_inherits WHERE inhrelid = (quote_ident(t.table_schema)||'.'||quote_ident(t.table_name))::regclass::oid))");

            return tables.Select(table => new DatabaseObjectNode(table, Table, parent, "mdi-table"));
        }

        private IEnumerable<DatabaseObjectNode> LoadTableGroupLabels(DatabaseObjectNode parent)
        {
            return new List<DatabaseObjectNode>
            {
                new DatabaseObjectNode(Label.Columns, GroupLabel, parent, "mdi-table-column"),
            };
        }

        private async Task<IEnumerable<DatabaseObjectNode>> LoadColumns(string database, DatabaseObjectNode parent)
        {
            string sql = 
                "SELECT column_name, data_type " +
                "FROM information_schema.columns " +
               $"WHERE table_schema = '{GetSchema(parent)}' " +
               $"AND table_name = '{GetTable(parent)}'";

            return (await _server.QueryForList(database, sql, reader => new { Name = reader.GetString(0), Type = reader.GetString(1) }))
                .Select(x => new DatabaseObjectNode(x.Name, Column, parent, "mdi-table-column", details: new Dictionary<string, object> { ["type"] = x.Type }));

        }

        private async Task<IEnumerable<DatabaseObjectNode>> LoadFunctions(string database, DatabaseObjectNode parent)
        {
            string sql;

            if (await GetVersion() < 11000)
            {
                sql = "SELECT proname, oidvectortypes(proargtypes) AS args " +
                      "FROM pg_proc INNER JOIN pg_namespace ns ON (pg_proc.pronamespace = ns.oid) " +
                      "LEFT JOIN pg_depend dep ON dep.objid = pg_proc.oid AND dep.deptype = 'e' " +
                     $"WHERE pg_proc.proisagg = false " +
                     $"AND ns.nspname = '{GetSchema(parent)}' " +
                     $"AND dep.objid IS NULL";
            }
            else
            {
                sql = "SELECT proname, oidvectortypes(proargtypes) AS args " +
                      "FROM pg_proc INNER JOIN pg_namespace ns ON (pg_proc.pronamespace = ns.oid) " +
                      "LEFT JOIN pg_depend dep ON dep.objid = pg_proc.oid AND dep.deptype = 'e' " +
                     $"WHERE ns.nspname = '{GetSchema(parent)}' " +
                     $"AND dep.objid IS NULL " +
                     $"AND pg_proc.prokind = 'f'";
            }

            return (await _server.QueryForList(database, sql, reader => new { Name = reader.GetString(0), Args = reader.GetString(1) }))
                .Select(x => new DatabaseObjectNode(x.Name, Function, parent, "mdi-function", details: new Dictionary<string, object> { ["args"] = x.Args }));
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
