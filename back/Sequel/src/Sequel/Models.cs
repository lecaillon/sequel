using System.ComponentModel.DataAnnotations;

namespace Sequel.Models
{
    public class ServerConnection
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public DBMS Type { get; set; }
        [Required]
        public string ConnectionString { get; set; }
        public string Environment { get; set; }
    }
}
