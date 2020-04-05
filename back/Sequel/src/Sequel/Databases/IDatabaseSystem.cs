using System.Collections.Generic;
using System.Threading.Tasks;
using Sequel.Models;

namespace Sequel.Databases
{
    public interface IDatabaseSystem
    {
        public DBMS Type { get; }

        public Task<IEnumerable<string>> LoadDatabasesAsync();
        public Task<IEnumerable<DatabaseObjectNode>> LoadDatabaseObjectsAsync(string database, DatabaseObjectNode? node);
    }
}
