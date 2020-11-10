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
        public abstract Task<string?> GetCurrentSchema(string? database);
        public abstract Task<IEnumerable<string>> LoadDatabases();
        public abstract Task<IEnumerable<string>> LoadSchemas(string? database);
        public abstract Task<IEnumerable<string>> LoadTables(string? database, string? schema);
        public abstract Task<IEnumerable<string>> LoadColumns(string? database, string? schema, string table);
        public abstract Task<IEnumerable<TreeViewNode>> LoadTreeViewNodes(string? database, TreeViewNode? node);
        public virtual async Task<List<TreeViewMenuItem>> LoadTreeViewMenuItems(TreeViewNode node, string? database, int connectionId)
        {
            var items = (await Store<TreeViewMenuItem>.GetList())
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
                var placeholders = await GetPlaceholders(node);
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

        public virtual async Task<IEnumerable<CompletionItem>> LoadCompletionItems(int lineNumber, int column, string? triggerCharacter, string? sql, string? database)
        {
            return await Helper.IgnoreErrorsAsync(async () =>
            {
                var items = new List<CompletionItem>();

                var statement = new Splitter().Process(sql).GetStatementAtPosition(lineNumber, column);
                if (statement is null)
                { // Empty statement => No suggestion
                    return items;
                }

                var currentToken = statement.GetCurrentToken();
                if (currentToken.IsComment)
                { // Positioned in a comment => No suggestion
                    return items;
                }

                var previousToken = statement.GetPreviousToken(skipMeaningless: true);
                if (previousToken is null)
                { // First token of the statement => No suggestion
                    return items;
                }

                if (previousToken.UpperText == "FROM" || previousToken.UpperText.EndsWith("JOIN"))
                { // Positioned after a "table previous keyword" (FROM or JOIN) => Suggest schemas AND tables
                    if (!string.IsNullOrEmpty(database))
                    {
                        var schemas = (await LoadSchemas(database)).ToList();
                        items.AddRange(schemas.Select(schema => new CompletionItem(schema, CompletionItemKind.Module)));
                    }

                    var tables = await LoadTables(database, await GetCurrentSchema(database));
                    items.AddRange(tables.Select(table => new CompletionItem(table, CompletionItemKind.Constant)));
                }

                if (currentToken.Text == ".")
                { // Positioned after a dot
                    previousToken = statement.GetPreviousToken(skipMeaningless: false)!;
                    var schemas = (await LoadSchemas(database)).ToList();
                    if (schemas.Contains(previousToken.Text))
                    { // Positioned after a "schema." => Suggest tables
                        var tables = await LoadTables(database, previousToken.Text);
                        items.AddRange(tables.Select(table => new CompletionItem(table, CompletionItemKind.Constant)));
                    }
                    else
                    { // Positioned after an "alias."
                        var tableAlias = statement.GetTableAlias(previousToken);
                        if (tableAlias != null)
                        { // => Suggest columns
                            if (tableAlias.Table is null)
                            {
                                items.AddRange(tableAlias.GetColumns().Select(column => new CompletionItem(column, CompletionItemKind.Field)));
                            }
                            else
                            {
                                var columns = await LoadColumns(database, tableAlias.Schema ?? await GetCurrentSchema(database), tableAlias.Table);
                                items.AddRange(columns.Select(column => new CompletionItem(column, CompletionItemKind.Field)));
                            }
                        }
                    }
                }

                return items;
            },
            new List<CompletionItem>());
        }

        public virtual IEnumerable<CodeLens> LoadCodeLens(string? sql)
        {
            return Helper.IgnoreErrors(() => 
                new Splitter().Process(sql).Select((stmt, i) => CodeLens.CreateExecuteBlockStatement(i, stmt.CodeLensLineNumber ?? -1)),
                new List<CodeLens>());
        }

        protected abstract Task<Dictionary<string, string>> GetPlaceholders(TreeViewNode node);
    }
}
