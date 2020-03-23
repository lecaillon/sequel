using System;
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
                { Type: GroupLabel, Name: Label.Schemas } => await LoadSchemas(database, parent),
                { Type: Schema } => LoadSchemaGroupLabels(parent),
                { Type: GroupLabel, Name: Label.Tables } => throw new NotImplementedException(),
                _ => LoadDatabase(database)
            };
        }

        private IEnumerable<DatabaseObjectNode> LoadDatabase(string database)
        {
            var rootNode = new DatabaseObjectNode(database, Database, parent: null, "mdi-database");
            rootNode.Children.Add(new DatabaseObjectNode(Label.Schemas, Schema, rootNode, "mdi-hexagon-multiple-outline"));

            return new List<DatabaseObjectNode> { rootNode };
        }

        private async Task<IEnumerable<DatabaseObjectNode>> LoadSchemas(string database, DatabaseObjectNode? root)
        {
            var schemas = await _server.QueryForListOfString(database, 
                "SELECT schema_name " +
                "FROM information_schema.schemata " +
                "WHERE schema_name NOT LIKE 'pg_%' " +
                "AND schema_name <> 'information_schema' " +
                "ORDER BY schema_name");

            return schemas.Select(schema => new DatabaseObjectNode(schema, Schema, root, "mdi-hexagon-multiple-outline"));
        }

        private IEnumerable<DatabaseObjectNode> LoadSchemaGroupLabels(DatabaseObjectNode? root)
        {
            return new List<DatabaseObjectNode>
            {
                new DatabaseObjectNode(Label.Tables, GroupLabel, root, "mdi-table")
            };
        }

        private static class Label
        {
            public const string Schemas = "Schemas";
            public const string Tables = "Tables";
        }
    }
}
