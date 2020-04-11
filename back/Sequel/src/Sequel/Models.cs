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
        [Required]
        public ServerConnection Server { get; set; } = default!;
        [Required]
        public string Database { get; set; } = default!;
        public DatabaseObjectNode? DatabaseObject { get; set; }
        public string? Sql { get; set; }
        public string? Id { get; set; }
    }

    public class QueryResponseContext
    {
        public QueryResponseContext(string id)
        {
            Id = Check.NotNullOrEmpty(id, nameof(id));
        }

        public string Id { get; }
        public bool Success { get; set; } = true;
        public string? Error { get; set; }
        public string Message => Error ?? GetSuccessMessage();
        public long Elapsed { get; set; } = 0;
        public int RecordsAffected { get; set; } = 0;
        public List<ColumnDefinition> Columns { get; } = new List<ColumnDefinition>();
        public List<object> Rows { get; } = new List<object>();
        public int RowCount => Rows.Count;

        private string GetSuccessMessage()
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

            return msg + $" in {Elapsed} ms";
        }
    }

    public class ColumnDefinition
    {
        public ColumnDefinition(string colId, string sqlType)
        {
            ColId = Check.NotNullOrEmpty(colId, nameof(colId));
            SqlType = Check.NotNullOrEmpty(sqlType, nameof(sqlType));
        }

        public string ColId { get; }
        public string HeaderName => ColId;
        public string Field => HeaderName;
        public string SqlType { get; }
        public bool Sortable { get; set; } = true;
        public bool Filter { get; set; } = true;
        public bool Editable { get; set; } = true;
        public bool Resizable { get; set; } = true;
        public int? Width => SqlType switch
        {
            "jsonb" => 200,
            "uuid" => 150,
            _ => null
        };
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
