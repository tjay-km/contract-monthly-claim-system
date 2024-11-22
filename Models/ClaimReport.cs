using System;
using System.Collections.Generic;

namespace CMCS.Models
{
    public class ClaimReport
    {
        public int Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string GeneratedBy { get; set; }
        public DateTime GeneratedDate { get; set; }
        public string ReportType { get; set; } // "Invoice" or "Summary"
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }
        public int TotalClaims { get; set; }
        public string FilePath { get; set; }
        public virtual ICollection<Claim> Claims { get; set; }
    }

    public class ClaimStatistics
    {
        public int TotalClaims { get; set; }
        public int AutoApprovedClaims { get; set; }
        public int PendingClaims { get; set; }
        public int RejectedClaims { get; set; }
        public decimal TotalAmountApproved { get; set; }
        public decimal AverageClaimAmount { get; set; }
        public decimal AutoApprovalRate => TotalClaims > 0 ? 
            (decimal)AutoApprovedClaims / TotalClaims * 100 : 0;
    }
}
