using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace CMCS.Models
{
    public class Department
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string Code { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public virtual ICollection<Module>? Modules { get; set; }

        public Department()
        {
            Modules = new List<Module>();
        }
    }
}
