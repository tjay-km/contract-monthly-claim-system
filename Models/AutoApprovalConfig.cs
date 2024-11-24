using System;
using System.ComponentModel.DataAnnotations;

namespace CMCS.Models
{
    public class AutoApprovalConfig
    {
        public int Id { get; set; }
        public decimal MaxAutoApprovalAmount { get; set; } = 5000m;
        public bool EnableAutoApproval { get; set; } = true;
        [Required]
        public string LastModifiedBy { get; set; } = string.Empty;
        [Required]
        public DateTime LastModifiedDate { get; set; } = DateTime.Now;
        [Required]
        public string Description { get; set; } = string.Empty;
    }
}
