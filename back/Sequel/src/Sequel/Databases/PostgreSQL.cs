using System;
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

        public Task<IEnumerable<DatabaseObjectNode>> LoadDatabaseObjects(string database)
        {
            throw new NotImplementedException();
        }
    }
}
