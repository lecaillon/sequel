using System.Collections.Generic;
using System.Threading.Tasks;
using Sequel.Models;

namespace Sequel.Databases
{
    public interface IDatabaseSystem
    {
        public DBMS Type { get; }

        public Task<IEnumerable<string>> LoadDatabases();
        public Task<IEnumerable<DatabaseObjectNode>> LoadDatabaseObjects(string database, DatabaseObjectNode? node);
    }
}
