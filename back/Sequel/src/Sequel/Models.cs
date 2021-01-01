using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Sequel.Core;
using Sequel.Core.Parser;
using Sequel.Databases;
using static Sequel.TreeViewNodeType;

namespace Sequel.Models
{
    public class ServerConnection : Identity
    {
        [Required]
        public string Name { get; set; } = default!;
        [Required]
        public DBMS Type { get; set; }
        [Required]
        public string ConnectionString { get; set; } = default!;
        [Required]
        public Env Environment { get; set; }
    }

    public class TreeViewNode
    {
        public const string PathSeparator = "::";

        public TreeViewNode() { }

        public TreeViewNode(string name,
                            TreeViewNodeType type,
                            TreeViewNode? parent,
                            string? icon = null,
                            string? color = null,
                            List<TreeViewNode> children = null!,
                            Dictionary<string, object> details = null!)
        {
            Id = parent is null ? name : $"{parent.Id}{PathSeparator}{name}";
            Type = type;
            Name = name;
            Icon = icon ?? Check.NotNullOrEmpty(parent?.Icon, nameof(parent.Icon));
            Color = color ?? Check.NotNullOrEmpty(parent?.Color, nameof(parent.Color));
            Children = children ?? new List<TreeViewNode>();
            Details = details ?? new Dictionary<string, object>();
        }

        [Required]
        public string Id { get; set; } = default!;
        [Required]
        public string Name { get; set; } = default!;
        [Required]
        public TreeViewNodeType Type { get; set; }
        [Required]
        public string Icon { get; set; } = default!;
        [Required]
        public string Color { get; set; } = default!;
        public List<TreeViewNode> Children { get; } = new List<TreeViewNode>();
        public Dictionary<string, object> Details { get; } = new Dictionary<string, object>();

        public string GetNameAtLevel(int level) => Id.Split(PathSeparator)[level];
    }

    public class TreeViewMenuItem
    {
        private int _order;

        public TreeViewMenuItem() { }

        public TreeViewMenuItem(string title, string command, string icon, int order, IEnumerable<DBMS>? dbms, IEnumerable<TreeViewNodeType>? nodeTypes)
        {
            Command = Check.NotNull(command, nameof(command));
            Title = Check.NotNull(title, nameof(title));
            Icon = Check.NotNull(icon, nameof(icon));
            Order = order;
            Dbms = dbms?.ToList() ?? new List<DBMS>();
            NodeTypes = nodeTypes?.ToList() ?? new List<TreeViewNodeType>();
        }
        // Menu
        public string Title { get; set; } = default!;
        public string Command { get; set; } = default!;
        public string Icon { get; set; } = default!;
        public int Order
        {
            get { return _order; }
            set { _order = value <= 0 ? int.MaxValue : value; }
        }
        public string? Confirmation { get; set; }
        // Targets
        public List<DBMS> Dbms { get; set; } = new List<DBMS>();
        public List<TreeViewNodeType> NodeTypes { get; set; } = new List<TreeViewNodeType>();
        public List<int> ConnectionIds { get; set; } = new List<int>();
        public List<string> Databases { get; set; } = new List<string>();
        public List<string> Nodes { get; set; } = new List<string>();

        internal static async Task ConfigureAsync()
        {
            await Store<TreeViewMenuItem>.Init(new List<TreeViewMenuItem>()
                .Union(Common.TreeViewMenuItems)
                .Union(PostgreSQL.TreeViewMenuItems)
                .Union(SqlServer.TreeViewMenuItems)
                .Union(SQLite.TreeViewMenuItems)
                .OrderBy(x => x.Order));
        }

        public static class Common
        {
            internal static readonly List<TreeViewMenuItem> TreeViewMenuItems = new List<TreeViewMenuItem>
            {
                new TreeViewMenuItem("All rows", "SELECT * FROM ${schema}.${table}", "mdi-database-search", 1000, new[] { DBMS.PostgreSQL, DBMS.SQLServer }, new[] { Table }),
            };
        }

        public static class PostgreSQL
        {
            internal static readonly List<TreeViewMenuItem> TreeViewMenuItems = new List<TreeViewMenuItem>
            {
                new TreeViewMenuItem("First 100 rows", "SELECT * FROM ${schema}.${table} LIMIT 100", "mdi-database-search", 2000, new[] { DBMS.PostgreSQL }, new[] { Table }),
            };
        }

        public static class SqlServer
        {
            internal static readonly List<TreeViewMenuItem> TreeViewMenuItems = new List<TreeViewMenuItem>
            {
                new TreeViewMenuItem("First 100 rows", "SELECT TOP 100 * FROM ${schema}.${table}", "mdi-database-search", 3000, new[] { DBMS.SQLServer }, new[] { Table }),
            };
        }

        public static class SQLite
        {
            internal static readonly List<TreeViewMenuItem> TreeViewMenuItems = new List<TreeViewMenuItem>
            {
                new TreeViewMenuItem("All rows", "SELECT * FROM ${table}", "mdi-database-search", 4000, new[] { DBMS.SQLite }, new[] { Table }),
            };
        }
    }

    public class QueryResponseContext
    {
        public QueryResponseContext(string id)
        {
            Id = Check.NotNull(id, nameof(id));
        }

        public string Id { get; }
        public QueryResponseStatus Status { get; set; } = QueryResponseStatus.Succeeded;
        public string? Error { get; set; }
        public int? ErrorPosition { get; set; } = null;
        public long Elapsed { get; set; }
        public int RecordsAffected { get; set; }
        public List<ColumnDefinition> Columns { get; } = new List<ColumnDefinition>();
        public List<object> Rows { get; } = new List<object>();
        public int RowCount => Rows.Count;
        public string Color => Status switch
        {
            QueryResponseStatus.Succeeded => "success",
            QueryResponseStatus.Canceled => "warning",
            _ => "error"
        };
        public string Message
        {
            get
            {
                if (Error != null)
                {
                    return Error;
                }
                else if (Status == QueryResponseStatus.Canceled)
                {
                    return $"Query canceled after {Elapsed} ms.{(RecordsAffected >= 0 ? $" {RecordsAffected} record(s) affected." : "")}";
                }
                else
                {
                    string msg = "";
                    if (RecordsAffected >= 0)
                    {
                        msg = $"{RecordsAffected} record(s) affected";
                    }
                    if (Columns.Count > 0)
                    {
                        msg += $"{(msg.Length == 0 ? "" : " and ")}{RowCount} row(s) returned";
                    }

                    return $"{msg} in {Elapsed} ms";
                }
            }
        }
    }

    public class QueryHistory
    {
        public QueryHistory(string code, QueryResponseStatus status, DBMS type, string sql, bool star, int executionCount, DateTime lastExecutedOn, string? name, string? keywords)
        {
            Code = Check.NotNullOrEmpty(code, nameof(code));
            Status = status;
            Type = type;
            Sql = Check.NotNullOrEmpty(sql, nameof(sql)); ;
            Star = star;
            ExecutionCount = executionCount;
            LastExecutedOn = lastExecutedOn;
            Name = name;
            Keywords = keywords?.Split(';').ToList() ?? new();
        }

        public string Code { get; }
        public QueryResponseStatus Status { get; private set; }
        public DBMS Type { get; }
        public string Sql { get; }
        public bool Star { get; }
        public int ExecutionCount { get; private set; }
        public DateTime LastExecutedOn { get; private set; }
        public string? Name { get; }
        public List<string> Keywords { get; }

        public List<QueryStat> Stats { get; } = new();

        public static string GetCode(string hash, DBMS type) => hash + (int)type;

        public static QueryHistory Create(string code, string sql, QueryExecutionContext query, QueryResponseContext response)
        {
            var history = new QueryHistory(code,
                                           response.Status,
                                           query.Server.Type,
                                           sql,
                                           star: false,
                                           executionCount: 0,
                                           lastExecutedOn: DateTime.Now,
                                           name: null,
                                           keywords: null);

            history.UpdateStatistics(query, response);
            return history;
        }

        public void UpdateStatistics(QueryExecutionContext query, QueryResponseContext response)
        {
            var now = DateTime.Now;

            if (Status != QueryResponseStatus.Succeeded)
            {
                Status = response.Status;
            }
            ExecutionCount++;
            LastExecutedOn = now;
            Stats.Add(new (response.Status,
                           executedOn: now,
                           query.Server.Environment.ToString(),
                           query.Database,
                           query.Server.Name,
                           response.Elapsed,
                           response.RecordsAffected,
                           response.RowCount));
        }

        public class QueryStat
        {
            public QueryStat(QueryResponseStatus status, DateTime executedOn, string environment, string database, string serverConnection, long elapsed, int rowCount, int recordsAffected)
            {
                Status = status;
                ExecutedOn = executedOn;
                Environment = Check.NotNullOrEmpty(environment, nameof(environment));
                Database = Check.NotNullOrEmpty(database, nameof(database));
                ServerConnection = Check.NotNullOrEmpty(serverConnection, nameof(serverConnection));
                Elapsed = elapsed;
                RowCount = rowCount;
                RecordsAffected = recordsAffected;
            }

            public QueryResponseStatus Status { get; }
            public DateTime ExecutedOn { get; }
            public string Environment { get; }
            public string Database { get; }
            public string ServerConnection { get; }
            public long Elapsed { get; }
            public int RowCount { get; }
            public int RecordsAffected { get; }
        }
    }

    public class QueryHistoryQuery
    {
        public string? Sql { get; set; }
        public bool ShowErrors { get; set; }
        public bool ShowFavorites { get; set; }
        public bool Star { get; set; } // used to add or remove a favorite
    }

    public class ColumnDefinition
    {
        static readonly HashSet<string> NumericSqlTypes = new HashSet<string> 
        {
            "smallint", "integer", "bigint", "numeric", "real", "double precision", "smallserial", "serial",
            "bigserial", "bigint", "bit", "decimal", "int", "money", "smallmoney", "tinyint", "float", "real"
        };

        public ColumnDefinition(string colId, string sqlType, string? headerName = null)
        {
            ColId = Check.NotNull(colId, nameof(colId));
            SqlType = Check.NotNull(sqlType, nameof(sqlType));
            HeaderName = headerName ?? ColId;
        }

        public string ColId { get; }
        public string Field => ColId;
        public string HeaderName { get; }
        public string SqlType { get; }
        public string? Type => NumericSqlTypes.Contains(SqlType) ? "numericColumn" : null;
        public string HeaderTooltip => $"{HeaderName} : {SqlType}";
        public bool Sortable { get; set; } = true;
        public bool Editable { get; set; } = true;
        public bool Resizable { get; set; } = true;
        public bool Hide { get; set; } = false;
        public int? Width => SqlType.ToLower() switch
        {
            "jsonb" => 200,
            "uuid" => 150,
            _ => null
        };
        public string? CellRenderer { get; set; }
        public string? ValueFormatter { get; set; }
        private object? _filter = null;
        public object Filter
        {
            get
            {
                if (_filter != null)
                {
                    return _filter;
                }
                else if (NumericSqlTypes.Contains(SqlType))
                {
                    return "agNumberColumnFilter";
                }
                else if (SqlType.Contains("date") || SqlType.Contains("timestamp"))
                {
                    return false;
                }
                else
                {
                    return "agTextColumnFilter";
                }
            }
            set { _filter = value; }
        }
    }

    public class CompletionItem
    {
        public CompletionItem(string label, CompletionItemKind kind, string? insertText = null, string? detail = null)
        {
            Label = label;
            Kind = kind;
            InsertText = insertText ?? label;
            Detail = detail;
        }

        public string Label { get; set; }
        public CompletionItemKind Kind { get; set; }
        public string InsertText { get; set; }
        public string? Detail { get; set; }
    }

    public class Snippet
    {
        public string Label { get; set; } = default!;
        public CompletionItemKind? Kind { get; set; }
        public string? InsertText { get; set; }
        public string? Detail { get; set; }
        public List<DBMS> Dbms { get; set; } = new List<DBMS>();
        public List<int> ConnectionIds { get; set; } = new List<int>();
        public List<string> Databases { get; set; } = new List<string>();

        internal static async Task ConfigureAsync()
        {
            if (!Store<Snippet>.Exists())
            {
                var list = SqlKeywords.Select(x => new Snippet
                {
                    Label = x,
                    Kind = CompletionItemKind.Keyword
                }).ToList();

                list.Add(new Snippet { Label = "s*", Kind = CompletionItemKind.Snippet, InsertText = "SELECT * FROM", Detail = "SELECT * FROM" });
                list.Add(new Snippet { Label = "sc*", Kind = CompletionItemKind.Snippet, InsertText = "SELECT COUNT(*) FROM", Detail = "SELECT COUNT(*) FROM" });

                await Store<Snippet>.Init(list);
            }
        }

        private static readonly List<string> SqlKeywords = new()
        {
            "ADD",
            "ADD CONSTRAINT",
            "ALL",
            "ALTER",
            "ALTER COLUMN",
            "ALTER TABLE",
            "AND",
            "ANY",
            "AS",
            "ASC",
            "BACKUP DATABASE",
            "BETWEEN",
            "CASE",
            "CHECK",
            "COLUMN",
            "CONSTRAINT",
            "CREATE",
            "CREATE DATABASE",
            "CREATE INDEX",
            "CREATE OR REPLACE VIEW",
            "CREATE PROCEDURE",
            "CREATE TABLE",
            "CREATE UNIQUE INDEX",
            "CREATE VIEW",
            "DATABASE",
            "DEFAULT",
            "DELETE",
            "DELETE FROM",
            "DESC",
            "DISTINCT",
            "DROP",
            "DROP COLUMN",
            "DROP CONSTRAINT",
            "DROP DATABASE",
            "DROP DEFAULT",
            "DROP INDEX",
            "DROP TABLE",
            "DROP VIEW",
            "EXEC",
            "EXISTS",
            "FOREIGN KEY",
            "FROM",
            "FULL OUTER JOIN",
            "GROUP BY",
            "HAVING",
            "IN",
            "INDEX",
            "INNER JOIN",
            "INSERT INTO",
            "INSERT INTO SELECT",
            "IS NULL",
            "IS NOT NULL",
            "JOIN",
            "LEFT JOIN",
            "LIKE",
            "LIMIT",
            "NOT",
            "NOT NULL",
            "OR",
            "ORDER BY",
            "OUTER JOIN",
            "PRIMARY KEY",
            "PROCEDURE",
            "RIGHT JOIN",
            "ROWNUM",
            "SELECT",
            "SELECT DISTINCT",
            "SELECT INTO",
            "SELECT TOP",
            "SET",
            "TABLE",
            "TOP",
            "TRUNCATE TABLE",
            "UNION",
            "UNION ALL",
            "UNIQUE",
            "UPDATE",
            "VALUES",
            "VIEW",
            "WHERE",
        };
    }

    public class CodeLens
    {
        public CodeLens(MonacoRange range, MonacoCommand command, string? id)
        {
            Id = id;
            Range = range;
            Command = command;
        }

        public MonacoRange Range { get; set; }
        public MonacoCommand Command { get; set; }
        public string? Id { get; set; }

        public static CodeLens CreateExecuteBlockStatement(int statementIndex, int startLineNumber)
            => new CodeLens(new MonacoRange(startLineNumber), new MonacoCommand("CmdExecuteBlockStmt", "Execute", new List<object> { statementIndex }), id: null);

        public class MonacoRange
        {
            public MonacoRange(int startLineNumber, int startColumn = 1, int endLineNumber = 0, int endColumn = 1)
            {
                StartLineNumber = startLineNumber;
                StartColumn = startColumn;
                EndLineNumber = endLineNumber == 0 ? StartLineNumber + 1 : endLineNumber;
                EndColumn = endColumn;
            }

            public int StartLineNumber { get; set; }
            public int StartColumn { get; set; }
            public int EndLineNumber { get; set; }
            public int EndColumn { get; set; }
        }

        public class MonacoCommand
        {
            public MonacoCommand(string id, string title, List<object> args, string? tooltip = null)
            {
                Id = id;
                Title = title;
                Tooltip = tooltip;
                Arguments = args ?? new List<object>();
            }

            public string Id { get; set; }
            public string Title { get; set; }
            public string? Tooltip { get; set; }
            public List<object> Arguments { get; }
        }
    }

    public abstract class Identity
    {
        public int? Id { get; set; }

        public Identity WithId(int id)
        {
            Id = id;
            return this;
        }

        public override bool Equals(object? obj)
        {
            if (obj is null || obj.GetType() != GetType())
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            return obj.GetType() == GetType() && Id == ((Identity)obj).Id;
        }

        public static bool operator ==(Identity? operand1, Identity? operand2)
        {
            if (operand1 is null)
            {
                return operand2 is null;
            }

            return operand1.Equals(operand2);
        }

        public static bool operator !=(Identity? operand1, Identity? operand2) => !(operand1 == operand2);

        public override int GetHashCode() => Id.GetHashCode();
    }

    public abstract class ContextBase
    {
        [Required]
        public ServerConnection Server { get; set; } = default!;

        [Required]
        public string Database { get; set; } = default!;
    }

    public class QueryExecutionContext : ContextBase
    {
        private string? _sqlStatement;

        /// <summary>
        ///     Sql statement(s).
        /// </summary>
        /// <remarks>
        ///     Should be private --> .NET 5
        /// </remarks>
        public string? Sql { get; set; }

        /// <summary>
        ///     Index of the statement to execute in the <see cref="Sql"/>.
        ///     If null execute everything.
        /// </summary>
        public int? StatementIndex { get; set; }

        /// <summary>
        ///     Sequel tab id.
        /// </summary>
        [Required]
        public string Id { get; set; } = default!;

        /// <summary>
        ///     Returns the <see cref="Sql"/> to execute, or just the statement at a given <see cref="StatementIndex"/>.
        /// </summary>
        public string? GetSqlStatement()
        {
            if (_sqlStatement is null)
            {

                if (Sql.IsNullOrEmpty() || StatementIndex is null)
                {
                    _sqlStatement = Sql;
                }
                else
                {
                    _sqlStatement = new Splitter().Process(Sql).ElementAt(StatementIndex.Value).ToString();
                }
            }

            return _sqlStatement;
        }
    }

    public class CompletionContext : ContextBase
    {
        /// <summary>
        ///     Line number (starts at 1)
        /// </summary>
        [Range(1, int.MaxValue)]
        public int LineNumber { get; set; }

        /// <summary>
        ///     Column (the first character in a line is between column 1 and column 2)
        /// </summary>
        [Range(1, int.MaxValue)]
        public int Column { get; set; }

        /// <summary>
        ///     Character that triggered the completion item provider
        /// </summary>
        public string? TriggerCharacter { get; set; }

        public string? Sql { get; set; }
    }

    public class TreeViewContext : ContextBase
    {
        /// <summary>
        ///     Selected node in the Sequel tree view
        /// </summary>
        public TreeViewNode? Node { get; set; }
    }
}
