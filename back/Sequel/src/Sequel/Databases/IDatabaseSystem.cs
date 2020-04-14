using System.Collections.Generic;
using System.Threading.Tasks;
using Sequel.Models;

namespace Sequel.Databases
{
    public interface IDatabaseSystem
    {
        public DBMS Type { get; }

        public Task<IEnumerable<string>> LoadDatabasesAsync();
        public Task<IEnumerable<DatabaseObjectNode>> LoadDatabaseObjectNodesAsync(string database, DatabaseObjectNode? node);
        public Task<IEnumerable<CompletionItem>> LoadIntellisenseAsync(string database);
    }
}
