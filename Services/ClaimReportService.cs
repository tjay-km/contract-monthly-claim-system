using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMCS.Data;
using CMCS.Models;
using CMCS.Models.Enums;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CMCS.Services
{
    public interface IClaimReportService
    {
        Task<ClaimReport> GenerateReportAsync(DateTime startDate, DateTime endDate, string reportType, string generatedBy);
        Task<ClaimReport?> GetReportAsync(int id);
        Task<IEnumerable<ClaimReport>> GetRecentReportsAsync(int count = 5);
        ClaimSummaryStatistics GetClaimStatistics(IReadOnlyCollection<Claim> claims);
    }

    public class ClaimReportService : IClaimReportService
    {
        public static readonly string[] ReportTypes = { "Summary", "Detailed", "Department" };

        private readonly ApplicationDbContext _context;
        private readonly ILogger<ClaimReportService> _logger;
        private readonly string _reportsDirectory;

        public ClaimReportService(ApplicationDbContext context, IWebHostEnvironment environment, ILogger<ClaimReportService> logger)
        {
            _context = context;
            _logger = logger;
            _reportsDirectory = Path.Combine(environment.ContentRootPath, "App_Data", "reports");
        }

        public async Task<ClaimReport?> GetReportAsync(int id) =>
            await _context.ClaimReports.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);

        public async Task<IEnumerable<ClaimReport>> GetRecentReportsAsync(int count = 5) =>
            await _context.ClaimReports
                .AsNoTracking()
                .OrderByDescending(r => r.GeneratedDate)
                .Take(count)
                .ToListAsync();

        public async Task<ClaimReport> GenerateReportAsync(DateTime startDate, DateTime endDate, string reportType, string generatedBy)
        {
            if (!ReportTypes.Contains(reportType))
                throw new ArgumentException($"Unknown report type '{reportType}'.", nameof(reportType));
            if (endDate < startDate)
                throw new ArgumentException("The end date must be on or after the start date.", nameof(endDate));

            var from = startDate.Date;
            var to = endDate.Date.AddDays(1);
            var claims = await _context.Claims
                .Where(c => c.SubmissionDate >= from && c.SubmissionDate < to)
                .OrderBy(c => c.SubmissionDate)
                .ToListAsync();

            var statistics = GetClaimStatistics(claims);

            Directory.CreateDirectory(_reportsDirectory);
            var filePath = Path.Combine(_reportsDirectory, $"Report_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}.pdf");
            WritePdf(filePath, BuildReportText(claims, statistics, reportType, from, endDate.Date));

            var report = new ClaimReport
            {
                StartDate = from,
                EndDate = endDate.Date,
                GeneratedDate = DateTime.Now,
                GeneratedBy = generatedBy,
                ReportType = reportType,
                Status = "Generated",
                FilePath = filePath,
                TotalClaims = statistics.TotalClaims,
                TotalAmount = statistics.TotalAmount,
                Claims = claims
            };
            _context.ClaimReports.Add(report);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Generated {ReportType} report {ReportId} covering {ClaimCount} claims",
                reportType, report.Id, claims.Count);
            return report;
        }

        public ClaimSummaryStatistics GetClaimStatistics(IReadOnlyCollection<Claim> claims)
        {
            return new ClaimSummaryStatistics
            {
                TotalClaims = claims.Count,
                TotalAmount = claims.Sum(c => c.TotalAmount),
                AverageAmount = claims.Count > 0 ? claims.Average(c => c.TotalAmount) : 0,
                PendingClaims = claims.Count(c => ClaimApprovalService.IsPending(c.Status)),
                ApprovedClaims = claims.Count(c => c.Status == ClaimStatus.Approved),
                RejectedClaims = claims.Count(c => c.Status == ClaimStatus.Rejected),
                ClaimsByDepartment = claims
                    .GroupBy(c => string.IsNullOrWhiteSpace(c.LecturerDepartment) ? "Unknown" : c.LecturerDepartment)
                    .ToDictionary(
                        g => g.Key,
                        g => new DepartmentStats { TotalClaims = g.Count(), TotalAmount = g.Sum(c => c.TotalAmount) })
            };
        }

        private static string BuildReportText(
            IReadOnlyCollection<Claim> claims, ClaimSummaryStatistics stats, string reportType, DateTime from, DateTime to)
        {
            var text = new StringBuilder()
                .AppendLine($"Claims Report ({reportType})")
                .AppendLine($"Period: {from:yyyy-MM-dd} to {to:yyyy-MM-dd}")
                .AppendLine($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}")
                .AppendLine()
                .AppendLine("Summary")
                .AppendLine($"Total claims: {stats.TotalClaims}")
                .AppendLine($"Total amount: R{stats.TotalAmount:N2}")
                .AppendLine($"Average amount: R{stats.AverageAmount:N2}")
                .AppendLine($"Pending: {stats.PendingClaims}   Approved: {stats.ApprovedClaims}   Rejected: {stats.RejectedClaims}")
                .AppendLine();

            if (reportType is "Department" or "Detailed" && stats.ClaimsByDepartment.Count > 0)
            {
                text.AppendLine("By department");
                foreach (var (department, deptStats) in stats.ClaimsByDepartment.OrderBy(d => d.Key))
                    text.AppendLine($"{department}: {deptStats.TotalClaims} claims, R{deptStats.TotalAmount:N2}");
                text.AppendLine();
            }

            if (reportType == "Detailed")
            {
                text.AppendLine("Claims");
                foreach (var claim in claims)
                {
                    text.AppendLine($"#{claim.Id}  {claim.SubmissionDate:yyyy-MM-dd}  {claim.LecturerName} ({claim.LecturerDepartment})  " +
                                    $"{claim.ModuleCode}  R{claim.TotalAmount:N2}  {claim.Status}");
                }
            }

            return text.ToString();
        }

        private static void WritePdf(string filePath, string content)
        {
            using var writer = new PdfWriter(filePath);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf);
            document.Add(new Paragraph(content));
        }
    }

    public class ClaimSummaryStatistics
    {
        public int TotalClaims { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
        public int PendingClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public int RejectedClaims { get; set; }
        public Dictionary<string, DepartmentStats> ClaimsByDepartment { get; set; } = new();
    }

    public class DepartmentStats
    {
        public int TotalClaims { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
