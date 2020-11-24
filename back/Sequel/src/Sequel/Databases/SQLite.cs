using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Sequel.Core;
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

        public override async Task<IEnumerable<string>> LoadDatabases()
        {
            string? database = IgnoreErrors(() => Path.GetFileName(_server.ConnectionString.Replace("Data Source=", "")));
            return database is null
                ? await Task.FromResult(Enumerable.Empty<string>())
                : await Task.FromResult(new List<string> { database });
        }

        protected override IEnumerable<TreeViewNode> LoadDatabaseRootNode(string database)
        {
            var rootNode = new TreeViewNode(database, Database, parent: null, "mdi-database", "amber darken-1");
            rootNode.Children.Add(new TreeViewNode("Tables", Tables, rootNode, "mdi-table", "blue"));
            rootNode.Children.Add(new TreeViewNode("Views", Views, rootNode, "mdi-group", "indigo"));
            rootNode.Children.Add(new TreeViewNode("Sequences", Sequences, rootNode, "mdi-numeric", "light-blue"));

            return new List<TreeViewNode> { rootNode };
        }

        protected override int GetNodeTypeLevel(TreeViewNodeType node) => node switch
        {
            Database => 0,
            Table => 2,
            _ => throw new NotSupportedException($"TreeViewNodeType {node} not supported.")
        };

        protected override Task<string?> GetCurrentSchema(string database)
            => Task.FromResult<string?>("main");

        protected override Task<IEnumerable<string>> LoadSchemas(string database)
            => Task.FromResult(Enumerable.Empty<string>());

        protected override async Task<IEnumerable<string>> LoadTables(string database, string? schema)
            => await _server.QueryStringList(database, $"SELECT tbl_name FROM sqlite_master WHERE type = 'table'");

        protected override async Task<IEnumerable<string>> LoadViews(string database, string? schema)
            => await _server.QueryStringList(database, $"SELECT tbl_name FROM sqlite_master WHERE type = 'view'");

        protected override async Task<IEnumerable<string>> LoadTableColumns(string database, string? schema, string table)
            => await _server.QueryStringList(database, $"SELECT name FROM pragma_table_info('{table}')");

        protected override async Task<IEnumerable<string>> LoadIndexes(string database, string? schema, string table)
            => await _server.QueryStringList(database, $"SELECT name FROM sqlite_master WHERE type = 'index' AND tbl_name = '{table}' ORDER BY name");

        protected override async Task<IEnumerable<string>> LoadViewColumns(string database, string? schema, string view)
            => await _server.QueryStringList(database, $"SELECT name FROM pragma_table_info('{view}')");

        protected override Task<IEnumerable<string>> LoadFunctions(string database, string? schema)
            => throw new NotSupportedException();

        protected override async Task<IEnumerable<string>> LoadSequences(string database, string? schema)
            => await _server.QueryStringList(database, $"SELECT name FROM sqlite_sequence ORDER BY name");

        protected override Task<IEnumerable<string>> LoadProcedures(string database, string? schema)
            => throw new NotSupportedException();
    }
}
