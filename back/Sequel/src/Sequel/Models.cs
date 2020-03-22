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
        public DatabaseObjectNode(NodeType type, string name, string icon, List<DatabaseObjectNode> children = null!)
        {
            Type = type;
            Name = name;
            Icon = icon;
            Children = children ?? new List<DatabaseObjectNode>();
        }

        [Required]
        public NodeType Type { get; }
        [Required]
        public string Name { get; }
        [Required]
        public string Icon { get; }
        public List<DatabaseObjectNode> Children { get; } = new List<DatabaseObjectNode>();
    }

    public class QueryExecutionContext
    {
        [Required]
        public ServerConnection Server { get; set; } = default!;
        [Required]
        public string Database { get; set; } = default!;
        public string? Schema { get; set; }
        public DatabaseObjectNode? DatabaseObject { get; set; }
        public string? Sql { get; set; }
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
