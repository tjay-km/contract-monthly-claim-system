using System;

namespace CMCS.Models
{
    public class AutoApprovalConfig
    {
        public int Id { get; set; }
        public decimal MaxAutoApprovalAmount { get; set; } = 5000m;
        public bool EnableAutoApproval { get; set; } = true;
        public string LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public string Description { get; set; }
    }
}
