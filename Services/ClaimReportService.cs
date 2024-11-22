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
            return await _context.ClaimReports
                .Include(r => r.Claims)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<IEnumerable<ClaimReport>> GetRecentReportsAsync(int count = 5)
        {
            return await _context.ClaimReports
                .OrderByDescending(r => r.GeneratedDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task<ClaimReport> GenerateReportAsync(DateTime startDate, DateTime endDate, string reportType, string generatedBy)
        {
            try 
            {
                System.Diagnostics.Debug.WriteLine($"Starting report generation - Type: {reportType}, Start: {startDate}, End: {endDate}");
                Console.WriteLine($"Starting report generation - Type: {reportType}, Start: {startDate}, End: {endDate}");

                // Debugging: Log all claims in the database with more details
                var allClaims = await _context.Claims
                    .Include(c => c.User)  // Ensure related user data is loaded
                    .ToListAsync();

                System.Diagnostics.Debug.WriteLine($"Total Claims in Database: {allClaims.Count}");
                Console.WriteLine($"Total Claims in Database: {allClaims.Count}");

                foreach (var claim in allClaims)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Claim - ID: {claim.Id}, " +
                        $"Lecturer: {claim.LecturerName}, " +
                        $"Date: {claim.SubmissionDate}, " +
                        $"Amount: {claim.TotalAmount}, " +
                        $"Status: {claim.Status}, " +
                        $"User: {claim.User?.Name ?? "No User"}"
                    );
                    Console.WriteLine(
                        $"Claim - ID: {claim.Id}, " +
                        $"Lecturer: {claim.LecturerName}, " +
                        $"Date: {claim.SubmissionDate}, " +
                        $"Amount: {claim.TotalAmount}, " +
                        $"Status: {claim.Status}, " +
                        $"User: {claim.User?.Name ?? "No User"}"
                    );
                }

                // More flexible claim filtering
                var claims = allClaims
                    .Where(c => 
                        c.SubmissionDate.Date >= startDate.Date && 
                        c.SubmissionDate.Date <= endDate.Date
                    )
                    .OrderBy(c => c.SubmissionDate)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"Found {claims.Count} claims in the specified date range");
                Console.WriteLine($"Found {claims.Count} claims in the specified date range");

                var report = new ClaimReport
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    GeneratedDate = DateTime.Now,
                    GeneratedBy = generatedBy,
                    ReportType = reportType,
                    Status = "Generated",
                    Claims = claims
                };

                var statistics = await CalculateStatisticsAsync(claims);
                var reportContent = GenerateReportContent(claims, statistics, reportType);

                // Improved file path generation with more robust error handling
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var reportsDirectory = Path.Combine(
                    baseDirectory, 
                    "Reports", 
                    DateTime.Now.Year.ToString(), 
                    DateTime.Now.Month.ToString()
                );
                
                // Ensure directory exists with proper error handling
                try 
                {
                    Directory.CreateDirectory(reportsDirectory);
                    System.Diagnostics.Debug.WriteLine($"Created reports directory: {reportsDirectory}");
                    Console.WriteLine($"Created reports directory: {reportsDirectory}");
                }
                catch (Exception dirEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error creating reports directory: {dirEx.Message}");
                    Console.WriteLine($"Error creating reports directory: {dirEx.Message}");
                    throw;
                }

                var fileName = $"Report_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}.pdf";
                var filePath = Path.Combine(reportsDirectory, fileName);

                try 
                {
                    // Use a PDF generation library
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        var pdf = new PdfDocument(new PdfWriter(stream));
                        var document = new Document(pdf);
                        
                        document.Add(new Paragraph(reportContent));
                        document.Close();
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Report PDF created: {filePath}");
                    Console.WriteLine($"Report PDF created: {filePath}");

                    report.FilePath = filePath;
                }
                catch (Exception fileEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error writing report file: {fileEx.Message}");
                    Console.WriteLine($"Error writing report file: {fileEx.Message}");
                    
                    report.Status = "Failed";
                    report.FilePath = null;
                    
                    // Log to a file
                    try 
                    {
                        var logPath = Path.Combine(baseDirectory, "report_generation_errors.txt");
                        await System.IO.File.AppendAllTextAsync(logPath, 
                            $"{DateTime.Now}: Report File Creation Error\n" +
                            $"Message: {fileEx.Message}\n" +
                            $"Stack Trace: {fileEx.StackTrace}\n\n");
                    }
                    catch { }

                    throw;
                }

                _context.ClaimReports.Add(report);
                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"Report saved to database - ID: {report.Id}");
                Console.WriteLine($"Report saved to database - ID: {report.Id}");

                return report;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Comprehensive Report Generation Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine($"Comprehensive Report Generation Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                // Log to a file for additional debugging
                try 
                {
                    var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "report_service_errors.txt");
                    await System.IO.File.AppendAllTextAsync(logPath, 
                        $"{DateTime.Now}: Comprehensive Report Generation Error\n" +
                        $"Message: {ex.Message}\n" +
                        $"Stack Trace: {ex.StackTrace}\n\n");
                }
                catch { }

                throw; // Rethrow to allow controller to handle
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
            config.LastModifiedDate = DateTime.Now;
            config.LastModifiedBy = modifiedBy;

            _context.AutoApprovalConfigs.Add(config);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string> GetReportFilePath(int reportId)
        {
            var report = await _context.ClaimReports.FindAsync(reportId);
            return report?.FilePath;
        }

        public async Task<AutoApprovalConfig> GetAutoApprovalConfigAsync()
        {
            return await _context.AutoApprovalConfigs
                .OrderByDescending(c => c.LastModifiedDate)
                .FirstOrDefaultAsync();
        }

        public async Task<ClaimStatistics> GetClaimStatisticsAsync(List<Claim> claims)
        {
            return await CalculateStatisticsAsync(claims);
        }

        private string GenerateReportContent(List<Claim> claims, ClaimStatistics statistics, string reportType)
        {
            // Debugging: Log statistics details
            System.Diagnostics.Debug.WriteLine($"Generating Report Content - Report Type: {reportType}");
            System.Diagnostics.Debug.WriteLine($"Total Claims: {statistics.TotalClaims}");
            System.Diagnostics.Debug.WriteLine($"Total Amount: {statistics.TotalAmount}");
            System.Diagnostics.Debug.WriteLine($"Pending Claims: {statistics.PendingClaims}");
            System.Diagnostics.Debug.WriteLine($"Approved Claims: {statistics.ApprovedClaims}");
            System.Diagnostics.Debug.WriteLine($"Rejected Claims: {statistics.RejectedClaims}");

            var content = $"Claims Report ({reportType})\n";
            content += $"Generated on: {DateTime.Now}\n\n";

            content += "Summary Statistics:\n";
            content += $"Total Claims: {statistics.TotalClaims}\n";
            content += $"Total Amount: R{statistics.TotalAmount:N2}\n";
            content += $"Average Amount: R{statistics.AverageAmount:N2}\n";
            content += $"Pending Claims: {statistics.PendingClaims}\n";
            content += $"Approved Claims: {statistics.ApprovedClaims}\n";
            content += $"Rejected Claims: {statistics.RejectedClaims}\n\n";

            // Only add department breakdown if there are claims
            if (statistics.ClaimsByDepartment != null && statistics.ClaimsByDepartment.Count > 0)
            {
                content += "Claims by Department:\n";
                foreach (var dept in statistics.ClaimsByDepartment)
                {
                    content += $"{dept.Key}: {dept.Value.TotalClaims} claims, R{dept.Value.TotalAmount:N2}\n";
                }
                content += "\n";
            }

            // Detailed claims section based on report type
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

            // Debugging: Log generated content
            System.Diagnostics.Debug.WriteLine("Generated Report Content:");
            System.Diagnostics.Debug.WriteLine(content);

            return content;
        }

        private async Task<ClaimStatistics> CalculateStatisticsAsync(List<Claim> claims)
        {
            // Debugging: Log input claims
            System.Diagnostics.Debug.WriteLine($"Calculating Statistics for {claims.Count} claims");

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
                ClaimsByDepartment = claims
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

            // Debugging: Log calculated statistics
            System.Diagnostics.Debug.WriteLine($"Total Claims: {statistics.TotalClaims}");
            System.Diagnostics.Debug.WriteLine($"Total Amount: {statistics.TotalAmount}");
            System.Diagnostics.Debug.WriteLine($"Pending Claims: {statistics.PendingClaims}");
            System.Diagnostics.Debug.WriteLine($"Approved Claims: {statistics.ApprovedClaims}");
            System.Diagnostics.Debug.WriteLine($"Rejected Claims: {statistics.RejectedClaims}");

            return statistics;
        }
    }

    public class ClaimStatistics
    {
        public int TotalClaims { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
        public int PendingClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public int RejectedClaims { get; set; }
        public Dictionary<string, DepartmentStats> ClaimsByDepartment { get; set; }
        public Dictionary<string, int> ClaimsByStatus { get; set; }
    }

    public class DepartmentStats
    {
        public int TotalClaims { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
