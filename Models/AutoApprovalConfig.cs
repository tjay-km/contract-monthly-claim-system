using System;

namespace CMCS.Models
{
    public class AutoApprovalConfig
    {
        public AutoApprovalConfig()
        {
            LastModifiedBy = string.Empty;
            Description = string.Empty;
            LastModifiedDate = DateTime.Now;
            EnableAutoApproval = true;
            MaxAutoApprovalAmount = 5000m;
        }

        public int Id { get; set; }
        public decimal MaxAutoApprovalAmount { get; set; }
        public bool EnableAutoApproval { get; set; }
        public string LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public string Description { get; set; }
    }
}
