using System.Collections.Generic;
using System.Threading.Tasks;
using Sequel.Core;
using Sequel.Models;

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

        public async Task<IEnumerable<DatabaseObjectNode>> LoadDatabaseObjects(string database)
        {
            var nodes = new List<DatabaseObjectNode>
            {
                new DatabaseObjectNode(NodeType.Database, database, "mdi-database")
            };

            nodes[0].Children.Add(await LoadSchemas(database));

            return nodes;
        }

        private async Task<DatabaseObjectNode> LoadSchemas(string database)
        {
            var node = new DatabaseObjectNode(NodeType.Schema, "Schemas", "mdi-hexagon-multiple-outline");
            var schemas = await _server.QueryForListOfString(database, 
                "SELECT schema_name " +
                "FROM information_schema.schemata " +
                "WHERE schema_name NOT LIKE 'pg_%' " +
                "AND schema_name <> 'information_schema' " +
                "ORDER BY schema_name");

            foreach (var schema in schemas)
            {
                node.Children.Add(new DatabaseObjectNode(NodeType.Schema, schema, "mdi-hexagon-multiple-outline"));
            }

            return node;
        }
    }
}
