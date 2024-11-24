using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CMCS.Models;
using CMCS.Services;

namespace CMCS.Models.ViewModels
{
    public class HRDashboardViewModel
    {
        public HRDashboardViewModel()
        {
            RecentClaims = new List<Claim>();
            RecentReports = new List<ClaimReport>();
            PendingClaims = new List<Claim>();
            RecentAudits = new List<ClaimAudit>();
            DepartmentStats = new List<DepartmentStatsModel>();
            AutoApprovalConfig = new AutoApprovalConfig();
            UserSummaries = new List<UserSummaryViewModel>();
            Statistics = new ClaimStatisticsModel();
            StartDate = DateTime.Today.AddDays(-30);
            EndDate = DateTime.Today;
            ApprovedClaims = new List<Claim>();
            RejectedClaims = new List<Claim>();
        }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<Claim> RecentClaims { get; set; }
        public List<ClaimReport> RecentReports { get; set; }
        public List<Claim> PendingClaims { get; set; }
        public List<Claim> ApprovedClaims { get; set; }
        public List<Claim> RejectedClaims { get; set; }
        public List<ClaimAudit> RecentAudits { get; set; }
        public ClaimStatisticsModel Statistics { get; set; }
        public List<DepartmentStatsModel> DepartmentStats { get; set; }
        public AutoApprovalConfig AutoApprovalConfig { get; set; }
        public List<UserSummaryViewModel> UserSummaries { get; set; }

        public int TotalClaims { get; set; }
        public int PendingClaimsCount { get; set; }
        public int ApprovedClaimsCount { get; set; }
        public int RejectedClaimsCount { get; set; }
        public decimal TotalClaimAmount { get; set; }
    }
}
