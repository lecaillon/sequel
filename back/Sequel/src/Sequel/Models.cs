using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Sequel.Models
{
    public class ServerConnection : ValueObject
    {
        [Required]
        public string Name { get; set; } = default!;
        [Required]
        public DBMS Type { get; set; }
        [Required]
        public string ConnectionString { get; set; } = default!;
        public Env? Environment { get; set; }

        protected override IEnumerable<object?> GetAtomicValues()
        {
            yield return Name;
            yield return Environment;
        }
    }
}
