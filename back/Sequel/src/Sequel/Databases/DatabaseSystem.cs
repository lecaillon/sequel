using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sequel.Core;
using Sequel.Core.Parser;
using Sequel.Models;
using static Sequel.TreeViewNodeType;

namespace Sequel.Databases
{
    public abstract class DatabaseSystem
    {
        public abstract DBMS Type { get; }
        protected abstract Task<string?> GetCurrentSchema(string database);
        public abstract Task<IEnumerable<string>> LoadDatabases();
        protected abstract Task<IEnumerable<string>> LoadSchemas(string database);
        protected abstract Task<IEnumerable<string>> LoadTables(string database, string? schema);
        protected abstract Task<IEnumerable<string>> LoadFunctions(string database, string schema);
        protected abstract Task<IEnumerable<string>> LoadColumns(string database, string? schema, string table);

        public virtual async Task<IEnumerable<TreeViewNode>> LoadTreeViewNodes(string database, TreeViewNode? parent)
        {
            Check.NotNull(database, nameof(database));

            return parent?.Type switch
            {
                null => LoadDatabaseRootNode(database),

                Schemas => await LoadSchemaNodes(database, parent),
                Tables => await LoadTableNodes(database, parent),
                Functions => await LoadFunctionNodes(database, parent),
                Columns => await LoadColumnNodes(database, parent),

                Schema => LoadSchemaGroupLabels(parent),
                Table => LoadTableGroupLabels(parent),

                _ => new List<TreeViewNode>()
            };
        }

        protected virtual IEnumerable<TreeViewNode> LoadDatabaseRootNode(string database)
        {
            var rootNode = new TreeViewNode(database, Database, parent: null, "mdi-database", "amber darken-1");
            rootNode.Children.Add(new TreeViewNode(Schemas.ToString(), Schemas, rootNode, "mdi-hexagon-multiple-outline", "cyan"));

            return new List<TreeViewNode> { rootNode };
        }

        protected virtual async Task<IEnumerable<TreeViewNode>> LoadSchemaNodes(string database, TreeViewNode parent)
        {
            return (await LoadSchemas(database))
                .Select(schema => new TreeViewNode(schema, Schema, parent, "mdi-hexagon-multiple-outline", "cyan"));
        }

        protected virtual async Task<IEnumerable<TreeViewNode>> LoadTableNodes(string database, TreeViewNode parent)
        {
            return (await LoadTables(database, parent.GetNameAtLevel(GetNodeTypeLevel(Schema))))
                .Select(table => new TreeViewNode(table, Table, parent, "mdi-table", "blue"));
        }

        protected virtual async Task<IEnumerable<TreeViewNode>> LoadFunctionNodes(string database, TreeViewNode parent)
        {
            return (await LoadFunctions(database, parent.GetNameAtLevel(GetNodeTypeLevel(Schema))))
                .Select(function => new TreeViewNode(function, Function, parent, "mdi-function", "teal"));
        }

        protected virtual async Task<IEnumerable<TreeViewNode>> LoadColumnNodes(string database, TreeViewNode parent)
        {
            return (await LoadColumns(database, parent.GetNameAtLevel(GetNodeTypeLevel(Schema)), parent.GetNameAtLevel(GetNodeTypeLevel(Table))))
                .Select(column => new TreeViewNode(column, Column, parent, "mdi-table-column", "deep-purple"));
        }

        protected virtual IEnumerable<TreeViewNode> LoadSchemaGroupLabels(TreeViewNode parent) => new[]
        {
            new TreeViewNode(Tables.ToString(), Tables, parent, "mdi-table", "blue"),
            new TreeViewNode(Functions.ToString(), Functions, parent, "mdi-function", "teal")
        };

        protected virtual IEnumerable<TreeViewNode> LoadTableGroupLabels(TreeViewNode parent) => new[]
        {
            new TreeViewNode(Columns.ToString(), Columns, parent, "mdi-table-column", "deep-purple"),
        };

        public virtual async Task<List<TreeViewMenuItem>> LoadTreeViewMenuItems(TreeViewNode node, string database, int connectionId)
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

        public virtual async Task<IEnumerable<CompletionItem>> LoadCompletionItems(int lineNumber, int column, string? triggerCharacter, string? sql, string database)
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

        protected virtual int GetNodeTypeLevel(TreeViewNodeType node) => node switch
        {
            Database => 0,
            Schema => 2,
            Table => 4,
            Function => 4,
            Column => 6,
            _ => throw new NotSupportedException($"TreeViewNodeType {node} not supported.")
        };

        protected virtual Task<Dictionary<string, string>> GetPlaceholders(TreeViewNode node)
        {
            return Task.FromResult(new Dictionary<string, string>
            { // Depending the database, some TreeViewNodeType could be undefined.
                { "${schema}", Helper.IgnoreErrors(() => node.GetNameAtLevel(GetNodeTypeLevel(Schema)), "${schema}") },
                { "${table}", Helper.IgnoreErrors(() => node.GetNameAtLevel(GetNodeTypeLevel(Table)), "${table}") },
            });
        }
    }
}
