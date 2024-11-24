using System.ComponentModel.DataAnnotations;

namespace CMCS.Models
{
    public class Module
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(1, 120, ErrorMessage = "Credits must be between 1 and 120")]
        public int Credits { get; set; }

        public int DepartmentId { get; set; }
        public virtual Department? Department { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
