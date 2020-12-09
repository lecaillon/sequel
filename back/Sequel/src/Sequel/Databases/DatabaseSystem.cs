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
        protected abstract Task<IEnumerable<string>> LoadViews(string database, string? schema);
        protected abstract Task<IEnumerable<string>> LoadFunctions(string database, string? schema);
        protected abstract Task<IEnumerable<string>> LoadProcedures(string database, string? schema);
        protected abstract Task<IEnumerable<string>> LoadSequences(string database, string? schema);
        protected abstract Task<IEnumerable<string>> LoadTableColumns(string database, string? schema, string table);
        protected abstract Task<IEnumerable<string>> LoadIndexes(string database, string? schema, string table);
        protected abstract Task<IEnumerable<string>> LoadPrimaryKeys(string database, string? schema, string table);
        protected abstract Task<IEnumerable<string>> LoadForeignKeys(string database, string? schema, string table);
        protected abstract Task<IEnumerable<string>> LoadViewColumns(string database, string? schema, string table);

        public virtual async Task<IEnumerable<TreeViewNode>> LoadTreeViewNodes(string database, TreeViewNode? parent) => parent?.Type switch
        {
            null => LoadDatabaseRootNode(database),

            Schemas => await LoadSchemaNodes(database, parent),
            Tables => await LoadTableNodes(database, parent),
            Views => await LoadViewNodes(database, parent),
            Functions => await LoadFunctionNodes(database, parent),
            Procedures => await LoadProcedureNodes(database, parent),
            Sequences => await LoadSequenceNodes(database, parent),

            TableColumns => await LoadTableColumnNodes(database, parent),
            Indexes => await LoadIndexes(database, parent),
            ViewColumns => await LoadTableColumnNodes(database, parent),

            Schema => LoadSchemaGroupLabels(parent),
            Table => LoadTableGroupLabels(parent),
            View => LoadViewGroupLabels(parent),

            _ => new List<TreeViewNode>()
        };

        protected virtual IEnumerable<TreeViewNode> LoadDatabaseRootNode(string database)
        {
            var rootNode = new TreeViewNode(database, Database, parent: null, "mdi-database", "amber darken-1");
            rootNode.Children.Add(new TreeViewNode("Schemas", Schemas, rootNode, "mdi-hexagon-multiple-outline", "cyan"));

            return new List<TreeViewNode> { rootNode };
        }

        protected virtual IEnumerable<TreeViewNode> LoadSchemaGroupLabels(TreeViewNode parent) => new[]
{
            new TreeViewNode("Tables", Tables, parent, "mdi-table", "blue"),
            new TreeViewNode("Views", Views, parent, "mdi-group", "indigo"),
            new TreeViewNode("Functions", Functions, parent, "mdi-function", "teal"),
            new TreeViewNode("Procedures", Procedures, parent, "mdi-code-braces", "light-blue"),
            new TreeViewNode("Sequences", Sequences, parent, "mdi-numeric", "lime")
        };

        protected virtual IEnumerable<TreeViewNode> LoadTableGroupLabels(TreeViewNode parent) => new[]
        {
            new TreeViewNode("Columns", TableColumns, parent, "mdi-table-column", "deep-purple"),
            new TreeViewNode("Indexes", Indexes, parent, "mdi-sort-ascending", "pink darken-4"),
        };

        protected virtual IEnumerable<TreeViewNode> LoadViewGroupLabels(TreeViewNode parent) => new[]
{
            new TreeViewNode("Columns", ViewColumns, parent, "mdi-table-column", "deep-purple"),
        };

        protected virtual async Task<IEnumerable<TreeViewNode>> LoadSchemaNodes(string database, TreeViewNode parent)
            => (await LoadSchemas(database)).Select(schema => new TreeViewNode(schema, Schema, parent));

        protected virtual async Task<IEnumerable<TreeViewNode>> LoadTableNodes(string database, TreeViewNode parent)
        {
            return (await LoadTables(database, Helper.IgnoreErrors(() => parent.GetNameAtLevel(GetNodeTypeLevel(Schema)))))
                .Select(table => new TreeViewNode(table, Table, parent));
        }

        protected virtual async Task<IEnumerable<TreeViewNode>> LoadViewNodes(string database, TreeViewNode parent)
        {
            return (await LoadViews(database, Helper.IgnoreErrors(() => parent.GetNameAtLevel(GetNodeTypeLevel(Schema)))))
                .Select(view => new TreeViewNode(view, View, parent));
        }

        protected virtual async Task<IEnumerable<TreeViewNode>> LoadFunctionNodes(string database, TreeViewNode parent)
        {
            return (await LoadFunctions(database, Helper.IgnoreErrors(() => parent.GetNameAtLevel(GetNodeTypeLevel(Schema)))))
                .Select(function => new TreeViewNode(function, Function, parent));
        }

        protected virtual async Task<IEnumerable<TreeViewNode>> LoadProcedureNodes(string database, TreeViewNode parent)
        {
            return (await LoadProcedures(database, Helper.IgnoreErrors(() => parent.GetNameAtLevel(GetNodeTypeLevel(Schema)))))
                .Select(proc => new TreeViewNode(proc, Procedure, parent));
        }

        protected virtual async Task<IEnumerable<TreeViewNode>> LoadSequenceNodes(string database, TreeViewNode parent)
        {
            return (await LoadSequences(database, Helper.IgnoreErrors(() => parent.GetNameAtLevel(GetNodeTypeLevel(Schema)))))
                .Select(seq => new TreeViewNode(seq, Sequence, parent));
        }

        protected virtual async Task<IEnumerable<TreeViewNode>> LoadTableColumnNodes(string database, TreeViewNode parent)
        {
            string? schema = Helper.IgnoreErrors(() => parent.GetNameAtLevel(GetNodeTypeLevel(Schema)));
            string table = parent.GetNameAtLevel(GetNodeTypeLevel(Table));

            var pks = await LoadPrimaryKeys(database, schema, table);
            var fks = (await LoadForeignKeys(database, schema, table)).Except(pks);
            var cols = (await LoadTableColumns(database, schema, table)).Except(pks).Except(fks);

            var list = fks.Select(fk => new TreeViewNode(fk, ForeignKey, parent, "mdi-key", "blue-grey lighten-3")) // fks
                .Union(cols.Select(column => new TreeViewNode(column, Column, parent))) // columns
                .OrderBy(x => x.Name)
                .ToList();

            list.InsertRange(0, pks.Select(pk => new TreeViewNode(pk, PrimaryKey, parent, "mdi-key", "yellow darken-1"))); // pks
            return list;
        }

        protected virtual async Task<IEnumerable<TreeViewNode>> LoadIndexes(string database, TreeViewNode parent)
        {
            return (await LoadIndexes(database, Helper.IgnoreErrors(() => parent.GetNameAtLevel(GetNodeTypeLevel(Schema))), parent.GetNameAtLevel(GetNodeTypeLevel(Table))))
                .Select(index => new TreeViewNode(index, TreeViewNodeType.Index, parent));
        }

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
                List<CompletionItem> items = new List<CompletionItem>();

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
                                var columns = await LoadTableColumns(database, tableAlias.Schema ?? await GetCurrentSchema(database), tableAlias.Table);
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
