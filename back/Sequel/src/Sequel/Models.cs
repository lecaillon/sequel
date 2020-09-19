using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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

    public class DatabaseObjectNode
    {
        public const string PathSeparator = "::";

        public DatabaseObjectNode() { }

        public DatabaseObjectNode(string name,
                                  DatabaseObjectType type,
                                  DatabaseObjectNode? parent,
                                  string icon,
                                  string color,
                                  List<DatabaseObjectNode> children = null!,
                                  Dictionary<string, object> details = null!)
        {
            Id = parent is null ? name : $"{parent.Id}{PathSeparator}{name}";
            Type = type;
            Name = name;
            Icon = icon;
            Color = color;
            Children = children ?? new List<DatabaseObjectNode>();
            Details = details ?? new Dictionary<string, object>();
        }

        [Required]
        public string Id { get; set; } = default!;
        [Required]
        public string Name { get; set; } = default!;
        [Required]
        public DatabaseObjectType Type { get; set; }
        [Required]
        public string Icon { get; set; } = default!;
        [Required]
        public string Color { get; set; } = default!;
        public List<DatabaseObjectNode> Children { get; } = new List<DatabaseObjectNode>();
        public Dictionary<string, object> Details { get; } = new Dictionary<string, object>();
    }

    public class QueryExecutionContext
    {
        private string? _database;

        [Required]
        public ServerConnection Server { get; set; } = default!;
        public string? Database
        {
            get { return Server.Type == DBMS.SQLite ? null : _database ; }
            set { _database = value; }
        }
        public DatabaseObjectNode? DatabaseObject { get; set; }
        public string? Sql { get; set; }
        public string? Id { get; set; }
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
            ExecutedOn = DateTime.UtcNow,
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
            ExecutedOn = DateTime.UtcNow;
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
        public object Filter
        {
            get
            {
                if (NumericSqlTypes.Contains(SqlType))
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
        }
        public string? CellRenderer { get; set; }
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
}
