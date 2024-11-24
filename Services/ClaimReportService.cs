using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using Microsoft.EntityFrameworkCore;
using CMCS.Models;
using CMCS.Models.Enums;
using CMCS.Data;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;

namespace CMCS.Services
{
    public interface IClaimReportService
    {
        Task<ClaimReport> GenerateReportAsync(DateTime startDate, DateTime endDate, string reportType, string generatedBy);
        Task<ClaimReport> GetReportAsync(int id);
        Task<IEnumerable<ClaimReport>> GetRecentReportsAsync(int count = 5);
        Task<AutoApprovalConfig> GetCurrentAutoApprovalConfigAsync();
        Task<bool> UpdateAutoApprovalConfigAsync(AutoApprovalConfig config, string modifiedBy);
        Task<string> GetReportFilePath(int reportId);
        Task<AutoApprovalConfig> GetAutoApprovalConfigAsync();
        Task<ClaimStatistics> GetClaimStatisticsAsync(List<Claim> claims);
    }

    public class ClaimReportService : IClaimReportService
    {
        private readonly ApplicationDbContext _context;

        public ClaimReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ClaimReport> GetReportAsync(int id)
        {
            var report = await _context.ClaimReports
                .Include(r => r.Claims)
                .FirstOrDefaultAsync(r => r.Id == id);

            return report ?? new ClaimReport();
        }

        public async Task<IEnumerable<ClaimReport>> GetRecentReportsAsync(int count = 5)
        {
            return await _context.ClaimReports
                .OrderByDescending(r => r.GeneratedDate)
                .Take(count)
                .ToListAsync() ?? new List<ClaimReport>();
        }

        public async Task<ClaimReport> GenerateReportAsync(DateTime startDate, DateTime endDate, string reportType, string generatedBy)
        {
            try 
            {
                System.Diagnostics.Debug.WriteLine($"Starting report generation - Type: {reportType}, Start: {startDate}, End: {endDate}");
                Console.WriteLine($"Starting report generation - Type: {reportType}, Start: {startDate}, End: {endDate}");

                var allClaims = await _context.Claims
                    .Include(c => c.User)
                    .Where(c => c != null && c.User != null)
                    .ToListAsync();

                System.Diagnostics.Debug.WriteLine($"Total Claims in Database: {allClaims.Count}");
                Console.WriteLine($"Total Claims in Database: {allClaims.Count}");

                foreach (var claim in allClaims.Where(c => c != null))
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Claim - ID: {claim.Id}, " +
                        $"Lecturer: {claim.LecturerName}, " +
                        $"Date: {claim.SubmissionDate}, " +
                        $"Amount: {claim.TotalAmount}, " +
                        $"Status: {claim.Status}, " +
                        $"User: {claim.User?.Name ?? "No User"}"
                    );
                }

                var claims = allClaims
                    .Where(c => 
                        c != null &&
                        c.SubmissionDate.Date >= startDate.Date && 
                        c.SubmissionDate.Date <= endDate.Date
                    )
                    .OrderBy(c => c.SubmissionDate)
                    .ToList();

                var report = new ClaimReport
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    GeneratedDate = DateTime.Now,
                    GeneratedBy = generatedBy ?? string.Empty,
                    ReportType = reportType ?? "Summary",
                    Status = "Generated",
                    Claims = claims ?? new List<Claim>()
                };

                var statistics = await CalculateStatisticsAsync(claims);
                var reportContent = GenerateReportContent(claims, statistics, reportType);

                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var reportsDirectory = Path.Combine(
                    baseDirectory, 
                    "Reports", 
                    DateTime.Now.Year.ToString(), 
                    DateTime.Now.Month.ToString()
                );
                
                Directory.CreateDirectory(reportsDirectory);

                var fileName = $"Report_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}.pdf";
                var filePath = Path.Combine(reportsDirectory, fileName);

                try 
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        var pdf = new PdfDocument(new PdfWriter(stream));
                        var document = new Document(pdf);
                        
                        document.Add(new Paragraph(reportContent ?? string.Empty));
                        document.Close();
                    }
                    
                    report.FilePath = filePath;
                }
                catch (Exception fileEx)
                {
                    report.Status = "Failed";
                    report.FilePath = string.Empty;
                    LogError("Error writing report file", fileEx);
                    throw;
                }

                _context.ClaimReports.Add(report);
                await _context.SaveChangesAsync();

                return report;
            }
            catch (Exception ex)
            {
                LogError("Comprehensive Report Generation Error", ex);
                throw;
            }
        }

        public async Task<AutoApprovalConfig> GetCurrentAutoApprovalConfigAsync()
        {
            return await _context.AutoApprovalConfigs
                .OrderByDescending(c => c.LastModifiedDate)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateAutoApprovalConfigAsync(AutoApprovalConfig config, string modifiedBy)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            
            if (string.IsNullOrEmpty(modifiedBy))
                throw new ArgumentException("Modified by cannot be empty", nameof(modifiedBy));

            config.LastModifiedDate = DateTime.Now;
            config.LastModifiedBy = modifiedBy;
            config.Description ??= string.Empty;

            _context.AutoApprovalConfigs.Add(config);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string> GetReportFilePath(int reportId)
        {
            var report = await _context.ClaimReports.FindAsync(reportId);
            return report?.FilePath ?? string.Empty;
        }

        public async Task<AutoApprovalConfig> GetAutoApprovalConfigAsync()
        {
            var config = await _context.AutoApprovalConfigs
                .OrderByDescending(c => c.LastModifiedDate)
                .FirstOrDefaultAsync();

            if (config == null)
            {
                config = new AutoApprovalConfig
                {
                    MaxAutoApprovalAmount = 5000m,
                    EnableAutoApproval = true,
                    LastModifiedBy = "System",
                    LastModifiedDate = DateTime.Now,
                    Description = "Default auto-approval configuration"
                };
                _context.AutoApprovalConfigs.Add(config);
                await _context.SaveChangesAsync();
            }

            return config;
        }

        public async Task<ClaimStatistics> GetClaimStatisticsAsync(List<Claim> claims)
        {
            if (claims == null)
                throw new ArgumentNullException(nameof(claims));

            return await CalculateStatisticsAsync(claims);
        }

        private string GenerateReportContent(List<Claim> claims, ClaimStatistics statistics, string reportType)
        {
            var content = $"Claims Report ({reportType})\n";
            content += $"Generated on: {DateTime.Now}\n\n";

            content += "Summary Statistics:\n";
            content += $"Total Claims: {statistics.TotalClaims}\n";
            content += $"Total Amount: R{statistics.TotalAmount:N2}\n";
            content += $"Average Amount: R{statistics.AverageAmount:N2}\n";
            content += $"Pending Claims: {statistics.PendingClaims}\n";
            content += $"Approved Claims: {statistics.ApprovedClaims}\n";
            content += $"Rejected Claims: {statistics.RejectedClaims}\n\n";

            if (statistics.DepartmentStatistics != null && statistics.DepartmentStatistics.Count > 0)
            {
                content += "Claims by Department:\n";
                foreach (var dept in statistics.DepartmentStatistics)
                {
                    content += $"{dept.Key}: {dept.Value.TotalClaims} claims, R{dept.Value.TotalAmount:N2}\n";
                }
                content += "\n";
            }

            if (reportType == "Detailed" && claims.Any())
            {
                content += "Detailed Claims:\n";
                foreach (var claim in claims)
                {
                    content += $"Claim ID: {claim.Id}\n";
                    content += $"Lecturer: {claim.LecturerName}\n";
                    content += $"Department: {claim.LecturerDepartment}\n";
                    content += $"Amount: R{claim.TotalAmount:N2}\n";
                    content += $"Status: {claim.Status}\n";
                    content += $"Submitted: {claim.SubmissionDate:d}\n\n";
                }
            }

            return content;
        }

        private async Task<ClaimStatistics> CalculateStatisticsAsync(List<Claim> claims)
        {
            var approvedClaims = claims.Where(c => c.Status == ClaimStatus.Approved).ToList();
            var pendingClaims = claims.Where(c => c.Status == ClaimStatus.Pending).ToList();
            var rejectedClaims = claims.Where(c => c.Status == ClaimStatus.Rejected).ToList();

            var statistics = new ClaimStatistics
            {
                TotalClaims = claims.Count,
                TotalAmount = claims.Sum(c => c.TotalAmount),
                AverageAmount = claims.Any() ? claims.Average(c => c.TotalAmount) : 0,
                PendingClaims = pendingClaims.Count,
                ApprovedClaims = approvedClaims.Count,
                RejectedClaims = rejectedClaims.Count,
                DepartmentStatistics = claims
                    .GroupBy(c => c.LecturerDepartment ?? "Unknown")
                    .ToDictionary(
                        g => g.Key,
                        g => new DepartmentStats
                        {
                            TotalClaims = g.Count(),
                            TotalAmount = g.Sum(c => c.TotalAmount)
                        }
                    ),
                ClaimsByStatus = new Dictionary<string, int>
                {
                    { ClaimStatus.Pending.ToString(), pendingClaims.Count },
                    { ClaimStatus.Approved.ToString(), approvedClaims.Count },
                    { ClaimStatus.Rejected.ToString(), rejectedClaims.Count }
                }
            };

            return statistics;
        }

        private async void LogError(string message, Exception ex)
        {
            try 
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "report_service_errors.txt");
                await File.AppendAllTextAsync(logPath, 
                    $"{DateTime.Now}: {message}\n" +
                    $"Message: {ex.Message}\n" +
                    $"Stack Trace: {ex.StackTrace}\n\n");
            }
            catch 
            {
                System.Diagnostics.Debug.WriteLine($"{message}: {ex.Message}");
                Console.WriteLine($"{message}: {ex.Message}");
            }
        }
    }

    public class ClaimStatistics
    {
        public int TotalClaims { get; set; }
        public int PendingClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public int RejectedClaims { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
        public Dictionary<string, DepartmentStats> DepartmentStatistics { get; set; } = new();
        public Dictionary<string, int> ClaimsByStatus { get; set; } = new();
    }

    public class DepartmentStats
    {
        public int TotalClaims { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
