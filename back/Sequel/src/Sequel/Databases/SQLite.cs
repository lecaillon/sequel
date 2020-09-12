using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Sequel.Models;
using static Sequel.Helper;

namespace Sequel.Databases
{
    public class SQLite : IDatabaseSystem
    {
        private readonly ServerConnection _server;

        public SQLite(ServerConnection server)
        {
            _server = Check.NotNull(server, nameof(server));
        }

        public DBMS Type => _server.Type;

        public async Task<IEnumerable<string>> LoadDatabasesAsync()
        {
            string? database = IgnoreErrors(() => Path.GetFileName(_server.ConnectionString.Replace("Data Source=", "")));
            return database is null 
                ? await Task.FromResult(Enumerable.Empty<string>()) 
                : await Task.FromResult(new List<string> { database });
        }

        public async Task<IEnumerable<DatabaseObjectNode>> LoadDatabaseObjectNodesAsync(string? database, DatabaseObjectNode? node)
        {
            return await Task.FromResult(new List<DatabaseObjectNode>());
        }

        public async Task<IEnumerable<CompletionItem>> LoadIntellisenseAsync(string? database)
        {
            return await Task.FromResult(new List<CompletionItem>());
        }
    }
}
