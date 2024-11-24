using System;
using System.Collections.Generic;
using CMCS.Services;
using CMCS.Models.Enums;
using CMCS.Models.ViewModels;

namespace CMCS.Models
{
    public class HRDashboardViewModel
    {
        public HRDashboardViewModel()
        {
            RecentAudits = new List<ClaimAudit>();
            RecentReports = new List<ClaimReport>();
            AutoApprovalConfig = new AutoApprovalConfig();
            Statistics = new ClaimStatistics();
            Lecturers = new List<LecturerManagementViewModel>();
            RecentClaims = new List<ClaimSummaryViewModel>();
            UserSummaries = new List<UserSummaryViewModel>();
        }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<ClaimAudit> RecentAudits { get; set; }
        public List<ClaimReport> RecentReports { get; set; }
        public AutoApprovalConfig AutoApprovalConfig { get; set; }
        public ClaimStatistics Statistics { get; set; }
        public List<LecturerManagementViewModel> Lecturers { get; set; }

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
        public string LecturerName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public ClaimStatus Status { get; set; }
        public DateTime SubmissionDate { get; set; }
    }

    public class UserSummaryViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public int TotalClaims { get; set; }
        public decimal TotalClaimAmount { get; set; }
    }
}
