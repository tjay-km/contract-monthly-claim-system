using System.Collections.Generic;

namespace CMCS.Models
{
    public class ClaimStatisticsModel
    {
        public int TotalClaims { get; set; }
        public int PendingClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public int RejectedClaims { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
        public Dictionary<string, DepartmentStatsModel> DepartmentStatistics { get; set; } = new();
        public Dictionary<string, int> ClaimsByStatus { get; set; } = new();
    }

    public class DepartmentStatsModel
    {
        public string Department { get; set; }
        public int TotalClaims { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
