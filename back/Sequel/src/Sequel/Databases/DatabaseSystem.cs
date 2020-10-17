using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sequel.Core;
using Sequel.Core.Parser;
using Sequel.Models;

namespace Sequel.Databases
{
    public abstract class DatabaseSystem
    {
        public abstract DBMS Type { get; }

        public virtual async Task<List<TreeViewMenuItem>> LoadTreeViewMenuItemsAsync(TreeViewNode node, string? database, int connectionId)
        {
            var items = (await Store<TreeViewMenuItem>.GetListAsync())
                .Where(x => (x.Dbms.IsNullOrEmpty() || x.Dbms.Contains(Type))
                         && (x.NodeTypes.IsNullOrEmpty() || x.NodeTypes.Contains(node.Type))
                         && (x.ConnectionIds.IsNullOrEmpty() || x.ConnectionIds.Contains(connectionId))
                         && (x.Databases.IsNullOrEmpty() || database is null || x.Databases.Contains(database))
                         && (x.Nodes.IsNullOrEmpty() || x.Nodes.Contains(node.Name)))
                .DistinctBy(x => new { x.Command, x.Title, x.Confirmation })
                .OrderBy(x => x.Order)
                .ToList();

            if (!items.IsNullOrEmpty())
            {
                var placeholders = await GetPlaceholdersAsync(node);
                foreach (var item in items)
                {
                    foreach (var entry in placeholders)
                    {
                        item.Command = item.Command.Replace(entry.Key, entry.Value);
                    }
                }
            }

            return items;
        }

        public abstract Task<IEnumerable<string>> LoadDatabasesAsync();
        public abstract Task<IEnumerable<TreeViewNode>> LoadTreeViewNodesAsync(string? database, TreeViewNode? node);
        public abstract Task<IEnumerable<CompletionItem>> LoadIntellisenseAsync(string? database);

        public virtual Task<IEnumerable<CodeLens>> LoadCodeLensAsync(string? sql)
        {
            if (sql.IsNullOrEmpty())
            {
                return Task.FromResult(Enumerable.Empty<CodeLens>());
            }

            try
            {
                return Task.FromResult(
                    new Splitter().Process(sql).Select((stmt, i) => CodeLens.CreateExecuteBlockStatement(i, stmt.StartLineNumber ?? -1)));
            }
            catch
            {
                return Task.FromResult(Enumerable.Empty<CodeLens>());
            }
        }

        protected abstract Task<Dictionary<string, string>> GetPlaceholdersAsync(TreeViewNode node);
    }
}
