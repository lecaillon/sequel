using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Sequel.Core;
using Sequel.Core.Parser;
using Sequel.Databases;

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
                            string icon,
                            string color,
                            List<TreeViewNode> children = null!,
                            Dictionary<string, object> details = null!)
        {
            Id = parent is null ? name : $"{parent.Id}{PathSeparator}{name}";
            Type = type;
            Name = name;
            Icon = icon;
            Color = color;
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
        public List<DBMS>? Dbms { get; set; } = new List<DBMS>();
        public List<TreeViewNodeType>? NodeTypes { get; set; } = new List<TreeViewNodeType>();
        public List<int>? ConnectionIds { get; set; } = new List<int>();
        public List<string>? Databases { get; set; } = new List<string>();
        public List<string>? Nodes { get; set; } = new List<string>();

        internal static async Task ConfigureAsync()
        {
            if (!Store<TreeViewMenuItem>.Exists())
            {
                await Store<TreeViewMenuItem>.Init(new List<TreeViewMenuItem>()
                    .Union(PostgreSQL.TreeViewMenuItems)
                    .Union(SQLite.TreeViewMenuItems)
                    .OrderBy(x => x.Order));
            }
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
        public int Id { get; set; }
        public DBMS Type { get; set; }
        public string ServerConnection { get; set; } = default!;
        public string Sql { get; set; } = default!;
        public string Hash { get; set; } = default!;
        public DateTime ExecutedOn { get; set; }
        public QueryResponseStatus Status { get; set; }
        public long Elapsed { get; set; }
        public int RowCount { get; set; }
        public int RecordsAffected { get; set; }
        public int ExecutionCount { get; set; }
        public bool Star { get; set; }

        public static QueryHistory Create(string sql, string hash, QueryExecutionContext query, QueryResponseContext response) => new QueryHistory
        {
            Type = query.Server.Type,
            ServerConnection = query.Server.Name,
            Sql = sql,
            Hash = hash,
            ExecutedOn = DateTime.Now,
            Status = response.Status,
            Elapsed = response.Elapsed,
            RowCount = response.RowCount,
            RecordsAffected = response.RecordsAffected,
            ExecutionCount = 1,
            Star = false
        };

        public void UpdateStatistics(QueryExecutionContext query, QueryResponseContext response)
        {
            ServerConnection = query.Server.Name;
            ExecutedOn = DateTime.Now;
            Status = response.Status;
            Elapsed = response.Elapsed;
            RowCount = response.RowCount;
            RecordsAffected = response.RecordsAffected;
            ExecutionCount++;
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
