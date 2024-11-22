using System;
using System.Collections.Generic;
using CMCS.Services;
using CMCS.Models.Enums;

namespace CMCS.Models
{
    public class HRDashboardViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<ClaimAudit> RecentAudits { get; set; }
        public List<ClaimReport> RecentReports { get; set; }
        public AutoApprovalConfig AutoApprovalConfig { get; set; }
        public ClaimStatistics Statistics { get; set; }

        public int TotalClaims { get; set; }
        public int PendingClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public int RejectedClaims { get; set; }
        public decimal TotalClaimAmount { get; set; }
        public List<ClaimSummaryViewModel> RecentClaims { get; set; }
        public List<UserSummaryViewModel> UserSummaries { get; set; }
    }

    public class ClaimSummaryViewModel
    {
        public int ClaimId { get; set; }
        public string LecturerName { get; set; }
        public string Department { get; set; }
        public decimal TotalAmount { get; set; }
        public ClaimStatus Status { get; set; }
        public DateTime SubmissionDate { get; set; }
    }

    public class UserSummaryViewModel
    {
        public string UserId { get; set; }
        public string Name { get; set; }
        public string Department { get; set; }
        public int TotalClaims { get; set; }
        public decimal TotalClaimAmount { get; set; }
    }
}
