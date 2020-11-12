using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Sequel.Models;
using static Sequel.Helper;
using static Sequel.TreeViewNodeType;

namespace Sequel.Databases
{
    public class SQLite : DatabaseSystem
    {
        internal static readonly List<TreeViewMenuItem> TreeViewMenuItems = new List<TreeViewMenuItem>
        {
            new TreeViewMenuItem("All rows", "SELECT * FROM ${table}", "mdi-database-search", 4000, new[] { DBMS.SQLite }, new[] { Table }),
        };

        private readonly ServerConnection _server;

        public SQLite(ServerConnection server)
        {
            _server = Check.NotNull(server, nameof(server));
        }

        public override DBMS Type => DBMS.SQLite;

        protected override Task<string?> GetCurrentSchema(string? database) => Task.FromResult<string?>("main");

        public override async Task<IEnumerable<string>> LoadDatabases()
        {
            string? database = IgnoreErrors(() => Path.GetFileName(_server.ConnectionString.Replace("Data Source=", "")));
            return database is null 
                ? await Task.FromResult(Enumerable.Empty<string>()) 
                : await Task.FromResult(new List<string> { database });
        }

        public override async Task<IEnumerable<TreeViewNode>> LoadTreeViewNodes(string? database, TreeViewNode? node)
        {
            return await Task.FromResult(new List<TreeViewNode>());
        }

        protected override Task<IEnumerable<string>> LoadSchemas(string? database)
        {
            throw new System.NotImplementedException();
        }

        protected override Task<IEnumerable<string>> LoadTables(string? database, string? schema)
        {
            throw new System.NotImplementedException();
        }

        protected override Task<IEnumerable<string>> LoadColumns(string? database, string? schema, string table)
        {
            throw new System.NotImplementedException();
        }

        protected override Task<IEnumerable<string>> LoadFunctions(string database, string schema)
        {
            throw new System.NotImplementedException();
        }
    }
}
