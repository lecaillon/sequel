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

        public async Task<IEnumerable<DatabaseObjectNode>> LoadDatabaseObjects(string database, DatabaseObjectNode? node)
        {
            return (node) switch
            {
                { Type: GroupLabel, Name: Label.Schemas } => await LoadSchemas(database),
                { Type: Schema } => LoadSchemaGroupLabels(),
                { Type: GroupLabel, Name: Label.Tables } => throw new NotImplementedException(),
                _ => LoadDatabase(database)
            };
        }

        private IEnumerable<DatabaseObjectNode> LoadDatabase(string database)
        {
            return new List<DatabaseObjectNode>
            {
                new DatabaseObjectNode(Database, database, "mdi-database", new List<DatabaseObjectNode>
                {
                    new DatabaseObjectNode(Schema, "Schemas", "mdi-hexagon-multiple-outline")
                })
            };
        }

        private async Task<IEnumerable<DatabaseObjectNode>> LoadSchemas(string database)
        {
            var schemas = await _server.QueryForListOfString(database, 
                "SELECT schema_name " +
                "FROM information_schema.schemata " +
                "WHERE schema_name NOT LIKE 'pg_%' " +
                "AND schema_name <> 'information_schema' " +
                "ORDER BY schema_name");

            return schemas.Select(schema => new DatabaseObjectNode(Schema, schema, "mdi-hexagon-multiple-outline"));
        }

        private IEnumerable<DatabaseObjectNode> LoadSchemaGroupLabels()
        {
            return new List<DatabaseObjectNode>
            {
                new DatabaseObjectNode(GroupLabel, Label.Tables, "mdi-table")
            };
        }

        private static class Label
        {
            public const string Schemas = "Schemas";
            public const string Tables = "Tables";
        }
    }
}
