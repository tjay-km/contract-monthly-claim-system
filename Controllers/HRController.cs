using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using System.Collections.Generic;
using CMCS.Data;
using CMCS.Models;
using CMCS.Models.Enums;
using CMCS.Models.ViewModels;
using CMCS.Services;
using Microsoft.EntityFrameworkCore;

namespace CMCS.Controllers
{
    [Authorize(Roles = "ProgrammeCoordinator,AcademicManager")]
    public class HRController : Controller
    {
        private readonly IClaimApprovalService _approvalService;
        private readonly IClaimAuditService _auditService;
        private readonly IClaimReportService _reportService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;

        public HRController(
            IClaimApprovalService approvalService,
            IClaimAuditService auditService,
            IClaimReportService reportService,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            ILogger<HRController> logger)
        {
            _approvalService = approvalService;
            _auditService = auditService;
            _reportService = reportService;
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        public IActionResult Reports()
        {
            var viewModel = new GenerateReportViewModel
            {
                StartDate = DateTime.Today.AddMonths(-1),
                EndDate = DateTime.Today,
                ReportTypes = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Summary", Text = "Summary Report", Selected = true },
                    new SelectListItem { Value = "Detailed", Text = "Detailed Report" },
                    new SelectListItem { Value = "Department", Text = "Department Report" }
                }
            };

            // Explicitly set the first item as selected
            viewModel.ReportType = "Summary";

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateReport(GenerateReportViewModel model)
        {
            // Extensive logging
            System.Diagnostics.Debug.WriteLine("GenerateReport method started");
            Console.WriteLine("GenerateReport method started");

            // Validate model state with detailed logging
            if (!ModelState.IsValid)
            {
                System.Diagnostics.Debug.WriteLine("ModelState is invalid");
                Console.WriteLine("ModelState is invalid");
                
                // Log specific model errors
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"Model Error: {error.ErrorMessage}");
                        Console.WriteLine($"Model Error: {error.ErrorMessage}");
                    }
                }

                // Log the invalid model state attempt
                await _auditService.LogAuditAsync(null, "Report Generation Failed", 
                    User.Identity.Name ?? "Unknown", 
                    $"Invalid Model State: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");

                return View("Reports", model);
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"Generating report - Type: {model.ReportType}, Start: {model.StartDate}, End: {model.EndDate}");
                Console.WriteLine($"Generating report - Type: {model.ReportType}, Start: {model.StartDate}, End: {model.EndDate}");

                var user = await _userManager.GetUserAsync(User);
                System.Diagnostics.Debug.WriteLine($"User retrieved: {user?.UserName ?? "NULL"}");
                Console.WriteLine($"User retrieved: {user?.UserName ?? "NULL"}");

                var report = await _reportService.GenerateReportAsync(
                    model.StartDate,
                    model.EndDate,
                    model.ReportType,
                    $"{user?.FirstName} {user?.LastName}"
                );

                System.Diagnostics.Debug.WriteLine($"Report generated - ID: {report.Id}, FilePath: {report.FilePath ?? "NULL"}");
                Console.WriteLine($"Report generated - ID: {report.Id}, FilePath: {report.FilePath ?? "NULL"}");

                // Log successful report generation
                await _auditService.LogAuditAsync(null, "Report Generated", 
                    User.Identity.Name ?? "Unknown", 
                    $"Report Type: {model.ReportType}, Period: {model.StartDate} - {model.EndDate}");

                // If report generation was successful and file exists, download the report
                if (!string.IsNullOrEmpty(report.FilePath) && System.IO.File.Exists(report.FilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"Attempting to download file: {report.FilePath}");
                    Console.WriteLine($"Attempting to download file: {report.FilePath}");

                    var fileBytes = await System.IO.File.ReadAllBytesAsync(report.FilePath);
                    var fileName = Path.GetFileName(report.FilePath);
                    
                    System.Diagnostics.Debug.WriteLine($"File download initiated: {fileName}");
                    Console.WriteLine($"File download initiated: {fileName}");

                    return File(fileBytes, "text/plain", fileName);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Report file not found or empty");
                    Console.WriteLine("Report file not found or empty");

                    // Log detailed error information
                    await _auditService.LogAuditAsync(null, "Report Generation Error", 
                        User.Identity.Name ?? "Unknown", 
                        $"Error Details: Report file not found or empty");

                    TempData["ErrorMessage"] = "Report could not be generated. Please check the file path.";
                    return RedirectToAction("Reports");
                }
            }
            catch (Exception ex)
            {
                // Extensive error logging
                System.Diagnostics.Debug.WriteLine($"Report Generation Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine($"Report Generation Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                // Log detailed error information
                await _auditService.LogAuditAsync(null, "Report Generation Error", 
                    User.Identity.Name ?? "Unknown", 
                    $"Error Details: {ex.Message}\nStack Trace: {ex.StackTrace}");

                // Log to a file for additional debugging
                try 
                {
                    var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");
                    await System.IO.File.AppendAllTextAsync(logPath, 
                        $"{DateTime.Now}: Report Generation Error\n" +
                        $"Message: {ex.Message}\n" +
                        $"Stack Trace: {ex.StackTrace}\n\n");
                }
                catch { }

                TempData["ErrorMessage"] = $"Failed to generate report: {ex.Message}";
                return RedirectToAction("Reports");
            }
        }

        public async Task<IActionResult> DownloadReport(int reportId)
        {
            try 
            {
                var report = await _reportService.GetReportAsync(reportId);
                if (report == null || string.IsNullOrEmpty(report.FilePath) || !System.IO.File.Exists(report.FilePath))
                {
                    TempData["ErrorMessage"] = "Report not found.";
                    return RedirectToAction("Reports");
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(report.FilePath);
                var fileName = Path.GetFileName(report.FilePath);
                return File(fileBytes, "text/plain", fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Report Download Error: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while downloading the report.";
                return RedirectToAction("Reports");
            }
        }

        private async Task<IActionResult> HandleUserAction(string userId, Func<ApplicationUser, Task> action)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            try
            {
                await action(user);
                return RedirectToAction(nameof(ManageLecturers));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing user action for {UserId}", userId);
                return BadRequest("Error performing action");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAutoApprovalConfig(AutoApprovalConfig config)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var success = await _reportService.UpdateAutoApprovalConfigAsync(config, user.UserName ?? "Unknown");
            if (!success)
            {
                return BadRequest("Failed to update auto-approval configuration");
            }

            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLecturer(EditLecturerViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var lecturer = await _userManager.FindByIdAsync(model.Id);
            if (lecturer == null)
                return NotFound();

            lecturer.Email = model.Email;
            lecturer.PhoneNumber = model.PhoneNumber;
            lecturer.FirstName = model.FirstName;
            lecturer.LastName = model.LastName;
            lecturer.Department = model.Department;

            await _userManager.UpdateAsync(lecturer);

            var user = await _userManager.GetUserAsync(User);
            await _auditService.LogAuditAsync(
                0,
                "Edit Lecturer",
                user?.Id ?? "Unknown",
                $"Updated lecturer details for {lecturer.FirstName} {lecturer.LastName}"
            );

            TempData["SuccessMessage"] = "Lecturer details updated successfully.";
            return RedirectToAction("ManageLecturers");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLecturer(string id)
        {
            var lecturer = await _userManager.FindByIdAsync(id);
            if (lecturer == null)
                return NotFound();

            var result = await _userManager.DeleteAsync(lecturer);
            if (result.Succeeded)
            {
                var user = await _userManager.GetUserAsync(User);
                await _auditService.LogAuditAsync(
                    0,
                    "Delete Lecturer",
                    user?.Id ?? "Unknown",
                    $"Deleted lecturer {lecturer.FirstName} {lecturer.LastName}"
                );

                TempData["SuccessMessage"] = "Lecturer deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete lecturer.";
            }

            return RedirectToAction("ManageLecturers");
        }

        public async Task<IActionResult> Dashboard()
        {
            var claims = await _context.Claims
                .Include(c => c.User)
                .ToListAsync();

            var users = await _userManager.Users.ToListAsync();

            var serviceStatistics = await _reportService.GetClaimStatisticsAsync(claims);
            
            // Convert service statistics to model statistics
            var modelStatistics = new ClaimStatisticsModel
            {
                TotalClaims = serviceStatistics.TotalClaims,
                PendingClaims = serviceStatistics.PendingClaims,
                ApprovedClaims = serviceStatistics.ApprovedClaims,
                RejectedClaims = serviceStatistics.RejectedClaims,
                TotalAmount = serviceStatistics.TotalAmount,
                AverageAmount = serviceStatistics.AverageAmount,
                DepartmentStatistics = serviceStatistics.DepartmentStatistics
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => new DepartmentStatsModel
                        {
                            Department = kvp.Key,
                            TotalClaims = kvp.Value.TotalClaims,
                            TotalAmount = kvp.Value.TotalAmount
                        }
                    ),
                ClaimsByStatus = serviceStatistics.ClaimsByStatus
            };
            
            // Group claims by department for department statistics
            var departmentStats = users
                .Where(u => u.Department != null)
                .GroupBy(u => u.Department)
                .Select(g => new DepartmentStatsModel
                {
                    Department = g.Key ?? "Unknown",
                    TotalClaims = claims.Count(c => c.User != null && c.User.Department == g.Key),
                    TotalAmount = claims
                        .Where(c => c.User != null && c.User.Department == g.Key)
                        .Sum(c => c.TotalAmount)
                })
                .ToList();

            var viewModel = new HRDashboardViewModel
            {
                StartDate = DateTime.Today.AddDays(-30),
                EndDate = DateTime.Today,
                RecentAudits = (await _auditService.GetRecentAuditsAsync()).ToList(),
                RecentReports = (await _reportService.GetRecentReportsAsync()).ToList(),
                AutoApprovalConfig = await _reportService.GetCurrentAutoApprovalConfigAsync(),
                Statistics = modelStatistics,
                TotalClaims = modelStatistics.TotalClaims,
                PendingClaimsCount = modelStatistics.PendingClaims,
                ApprovedClaimsCount = modelStatistics.ApprovedClaims,
                RejectedClaimsCount = modelStatistics.RejectedClaims,
                TotalClaimAmount = modelStatistics.TotalAmount,
                PendingClaims = claims.Where(c => c.Status == ClaimStatus.Pending).ToList(),
                ApprovedClaims = claims.Where(c => c.Status == ClaimStatus.Approved).ToList(),
                RejectedClaims = claims.Where(c => c.Status == ClaimStatus.Rejected).ToList(),
                RecentClaims = claims.OrderByDescending(c => c.SubmissionDate).Take(10).ToList(),
                DepartmentStats = departmentStats,
                UserSummaries = users
                    .Where(u => _userManager.IsInRoleAsync(u, "Lecturer").Result)
                    .Select(u => new UserSummaryViewModel
                    {
                        UserId = u.Id,
                        Name = $"{u.FirstName} {u.LastName}",
                        Department = u.Department ?? "Unknown", 
                        TotalClaims = claims.Count(c => c.UserId == u.Id),
                        TotalClaimAmount = claims
                            .Where(c => c.UserId == u.Id)
                            .Sum(c => c.TotalAmount)
                    })
                    .Where(summary => summary.TotalClaims > 0)
                    .OrderByDescending(summary => summary.TotalClaimAmount)
                    .ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveClaim(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var result = await _approvalService.ApproveClaimAsync(id, user.Id);

                await _auditService.LogAuditAsync(
                    id,
                    "Approve Claim",
                    user.Id,
                    $"Claim {id} approved by HR"
                );

                TempData["SuccessMessage"] = "Claim approved successfully.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Failed to approve claim.";
            }

            return RedirectToAction("Dashboard");
        }

        public async Task<IActionResult> AuditTrail(int claimId)
        {
            var audits = await _auditService.GetAuditTrailForClaimAsync(claimId);
            return View(audits);
        }

        [HttpGet]
        public async Task<IActionResult> ManageLecturers()
        {
            var lecturers = await _userManager.GetUsersInRoleAsync("Lecturer");
            return View(lecturers);
        }

        [HttpGet]
        public async Task<IActionResult> EditLecturer(string id)
        {
            var lecturer = await _userManager.FindByIdAsync(id);
            if (lecturer == null)
                return NotFound();

            var viewModel = new EditLecturerViewModel
            {
                Id = lecturer.Id,
                Email = lecturer.Email ?? string.Empty,
                PhoneNumber = lecturer.PhoneNumber ?? string.Empty,
                FirstName = lecturer.FirstName,
                LastName = lecturer.LastName,
                Department = lecturer.Department
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> ConfigureAutoApproval()
        {
            var config = await _reportService.GetAutoApprovalConfigAsync();
            return View(config);
        }

        [HttpPost]
        public async Task<IActionResult> ConfigureAutoApproval(AutoApprovalConfig config)
        {
            if (!ModelState.IsValid)
                return View(config);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _reportService.UpdateAutoApprovalConfigAsync(config, userId);

            // Log the configuration change
            await _auditService.LogAuditAsync(
                0, // Use 0 since we don't have a claim ID for config changes
                "Auto-Approval Configuration",
                userId ?? "Unknown",
                $"Updated auto-approval settings: Max Amount = R{config.MaxAutoApprovalAmount:N2}, Enabled = {config.EnableAutoApproval}"
            );

            TempData["SuccessMessage"] = "Auto-approval configuration updated successfully.";
            return RedirectToAction("Dashboard");
        }

        public async Task<IActionResult> CheckClaims()
        {
            var claims = await _context.Claims.ToListAsync();
            
            Console.WriteLine($"Total Claims: {claims.Count}");
            
            foreach (var claim in claims)
            {
                Console.WriteLine($"Claim ID: {claim.Id}, " +
                                $"Lecturer: {claim.LecturerName}, " +
                                $"Date: {claim.SubmissionDate}, " +
                                $"Amount: {claim.TotalAmount}, " +
                                $"Status: {claim.Status}");
            }
            
            return Ok($"Found {claims.Count} claims");
        }

        // GET: Dashboard
        public async Task<IActionResult> DashboardLecturer()
        {
            // Get statistics for the dashboard
            ViewBag.TotalClaims = await _context.Claims.CountAsync();
            ViewBag.ApprovedClaims = await _context.Claims.CountAsync(c => c.Status == ClaimStatus.Approved);
            ViewBag.PendingClaims = await _context.Claims.CountAsync(c => c.Status == ClaimStatus.Pending);
            ViewBag.TotalClaimAmount = await _context.Claims
                .Where(c => c.Status == ClaimStatus.Approved)
                .SumAsync(c => c.HoursWorked * c.HourlyRate);

            // Get recent claims
            ViewBag.RecentClaims = await _context.Claims
                .OrderByDescending(c => c.SubmissionDate)
                .Take(10)
                .Select(c => new {
                    c.Id,
                    c.LecturerName,
                    c.LecturerDepartment,
                    c.HoursWorked,
                    c.HourlyRate,
                    c.Status,
                    c.SubmissionDate
                })
                .ToListAsync();

            // Get user summaries
            ViewBag.UserSummaries = await _context.Claims
                .GroupBy(c => new { c.LecturerId, c.LecturerName, c.LecturerDepartment })
                .Select(g => new {
                    Name = g.Key.LecturerName,
                    Department = g.Key.LecturerDepartment,
                    TotalClaims = g.Count(),
                    TotalAmount = g.Sum(c => c.HoursWorked * c.HourlyRate)
                })
                .ToListAsync();

            // Get lecturers for management
            var lecturers = await _context.Users
                .Where(u => u.UserType == UserType.Lecturer)
                .Select(u => new LecturerManagementViewModel
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    Department = u.Department,
                    Specialization = u.Specialization,
                    IsActive = u.IsActive,
                    TotalClaims = _context.Claims.Count(c => c.LecturerId == u.Id),
                    TotalHours = _context.Claims
                        .Where(c => c.LecturerId == u.Id && c.Status == ClaimStatus.Approved)
                        .Sum(c => c.HoursWorked)
                })
                .ToListAsync();

            return View(lecturers);
        }

        // GET: Get Lecturer Details
        [HttpGet]
        public async Task<IActionResult> GetLecturer(string id)
        {
            var lecturer = await _context.Users
                .Where(u => u.Id == id && u.UserType == UserType.Lecturer)
                .Select(u => new LecturerManagementViewModel
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    Department = u.Department,
                    Specialization = u.Specialization,
                    IsActive = u.IsActive
                })
                .FirstOrDefaultAsync();

            if (lecturer == null)
            {
                return Json(new { success = false, message = "Lecturer not found" });
            }

            return Json(lecturer);
        }

        // POST: Update Lecturer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateLecturer(LecturerManagementViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Invalid model state" });
                }

                var lecturer = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == model.Id && u.UserType == UserType.Lecturer);

                if (lecturer == null)
                {
                    return Json(new { success = false, message = "Lecturer not found" });
                }

                // Update lecturer details
                lecturer.FirstName = model.FirstName;
                lecturer.LastName = model.LastName;
                lecturer.Email = model.Email;
                lecturer.PhoneNumber = model.PhoneNumber;
                lecturer.Department = model.Department;
                lecturer.Specialization = model.Specialization;

                // Log the audit
                await _auditService.LogAuditAsync(null, "Lecturer Updated", 
                    User.Identity.Name ?? "Unknown", 
                    $"Updated lecturer details for {lecturer.FirstName} {lecturer.LastName}");

                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Toggle Lecturer Status
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLecturerStatus(string id, bool activate)
        {
            try
            {
                var lecturer = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id && u.UserType == UserType.Lecturer);

                if (lecturer == null)
                {
                    return Json(new { success = false, message = "Lecturer not found" });
                }

                lecturer.IsActive = activate;

                // Log the audit
                await _auditService.LogAuditAsync(null, activate ? "Lecturer Activated" : "Lecturer Deactivated", 
                    User.Identity.Name ?? "Unknown", 
                    $"{(activate ? "Activated" : "Deactivated")} lecturer account for {lecturer.FirstName} {lecturer.LastName}");

                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
