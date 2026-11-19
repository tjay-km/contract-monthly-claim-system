using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CMCS.Data;
using CMCS.Models;
using CMCS.Models.Enums;
using CMCS.Models.ViewModels;
using CMCS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CMCS.Controllers
{
    /// <summary>Statistics, reports, lecturer management and auto-approval settings.</summary>
    [Authorize(Roles = "ProgrammeCoordinator,AcademicManager")]
    public class HRController : Controller
    {
        private const string LecturerRole = "Lecturer";

        private readonly IClaimAuditService _auditService;
        private readonly IClaimReportService _reportService;
        private readonly IAutoApprovalConfigService _autoApprovalConfig;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HRController> _logger;

        public HRController(
            IClaimAuditService auditService,
            IClaimReportService reportService,
            IAutoApprovalConfigService autoApprovalConfig,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            ILogger<HRController> logger)
        {
            _auditService = auditService;
            _reportService = reportService;
            _autoApprovalConfig = autoApprovalConfig;
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        // ---------------------------------------------------------------- dashboard
        public async Task<IActionResult> Dashboard()
        {
            var claims = await _context.Claims.AsNoTracking().ToListAsync();
            var lecturers = await _userManager.GetUsersInRoleAsync(LecturerRole);
            var stats = _reportService.GetClaimStatistics(claims);

            var viewModel = new HRDashboardViewModel
            {
                StartDate = DateTime.Today.AddDays(-30),
                EndDate = DateTime.Today,
                RecentAudits = (await _auditService.GetRecentAuditsAsync()).ToList(),
                RecentReports = (await _reportService.GetRecentReportsAsync()).ToList(),
                AutoApprovalConfig = await _autoApprovalConfig.GetCurrentConfigAsync(),
                Statistics = new ClaimStatistics
                {
                    TotalClaims = stats.TotalClaims,
                    AutoApprovedClaims = claims.Count(c => c.Status == ClaimStatus.Approved && c.ReviewerId == null),
                    PendingClaims = stats.PendingClaims,
                    RejectedClaims = stats.RejectedClaims,
                    TotalAmountApproved = claims.Where(c => c.Status == ClaimStatus.Approved).Sum(c => c.TotalAmount),
                    AverageClaimAmount = stats.AverageAmount
                },
                TotalClaims = stats.TotalClaims,
                PendingClaims = stats.PendingClaims,
                ApprovedClaims = stats.ApprovedClaims,
                RejectedClaims = stats.RejectedClaims,
                TotalClaimAmount = stats.TotalAmount,
                Lecturers = lecturers
                    .OrderBy(u => u.LastName)
                    .Select(u => new LecturerManagementViewModel
                    {
                        Id = u.Id,
                        FirstName = u.FirstName ?? string.Empty,
                        LastName = u.LastName ?? string.Empty,
                        Email = u.Email ?? string.Empty,
                        Department = u.Department ?? string.Empty,
                        Specialization = u.Specialization ?? string.Empty,
                        IsActive = u.IsActive
                    })
                    .ToList(),
                RecentClaims = claims
                    .OrderByDescending(c => c.SubmissionDate)
                    .Take(10)
                    .Select(c => new ClaimSummaryViewModel
                    {
                        ClaimId = c.Id,
                        LecturerName = c.LecturerName,
                        Department = c.LecturerDepartment,
                        TotalAmount = c.TotalAmount,
                        Status = c.Status,
                        SubmissionDate = c.SubmissionDate
                    })
                    .ToList(),
                UserSummaries = lecturers
                    .Select(u => new UserSummaryViewModel
                    {
                        UserId = u.Id,
                        Name = u.Name,
                        Department = u.Department ?? "Unknown",
                        TotalClaims = claims.Count(c => c.LecturerId == u.Id),
                        TotalClaimAmount = claims.Where(c => c.LecturerId == u.Id).Sum(c => c.TotalAmount)
                    })
                    .Where(summary => summary.TotalClaims > 0)
                    .OrderByDescending(summary => summary.TotalClaimAmount)
                    .ToList()
            };

            return View(viewModel);
        }

        // ------------------------------------------------------------------ reports
        public IActionResult Reports()
        {
            return View(WithReportTypes(new GenerateReportViewModel
            {
                StartDate = DateTime.Today.AddMonths(-1),
                EndDate = DateTime.Today,
                ReportType = "Summary"
            }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateReport(GenerateReportViewModel model)
        {
            if (model.EndDate < model.StartDate)
                ModelState.AddModelError(nameof(model.EndDate), "The end date must be on or after the start date.");
            if (!ModelState.IsValid)
                return View("Reports", WithReportTypes(model));

            var user = await _userManager.GetUserAsync(User);
            try
            {
                var report = await _reportService.GenerateReportAsync(
                    model.StartDate, model.EndDate, model.ReportType, user?.Name ?? User.Identity?.Name ?? "Unknown");

                await _auditService.LogAuditAsync(null, "Report Generated", user?.Name ?? "Unknown",
                    $"{model.ReportType} report for {model.StartDate:yyyy-MM-dd} to {model.EndDate:yyyy-MM-dd}");

                return File(await System.IO.File.ReadAllBytesAsync(report.FilePath), "application/pdf",
                    Path.GetFileName(report.FilePath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Report generation failed");
                TempData["ErrorMessage"] = "The report could not be generated. Please try again.";
                return RedirectToAction(nameof(Reports));
            }
        }

        public async Task<IActionResult> DownloadReport(int reportId)
        {
            var report = await _reportService.GetReportAsync(reportId);
            if (report == null || string.IsNullOrEmpty(report.FilePath) || !System.IO.File.Exists(report.FilePath))
            {
                TempData["ErrorMessage"] = "Report not found.";
                return RedirectToAction(nameof(Reports));
            }

            return PhysicalFile(report.FilePath, "application/pdf", Path.GetFileName(report.FilePath));
        }

        private static GenerateReportViewModel WithReportTypes(GenerateReportViewModel model)
        {
            model.ReportTypes = new List<SelectListItem>
            {
                new() { Value = "Summary", Text = "Summary Report" },
                new() { Value = "Detailed", Text = "Detailed Report" },
                new() { Value = "Department", Text = "Department Report" }
            };
            return model;
        }

        // ------------------------------------------------------------- auto-approval
        [HttpGet]
        public async Task<IActionResult> ConfigureAutoApproval()
        {
            return View(await _autoApprovalConfig.GetCurrentConfigAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfigureAutoApproval(AutoApprovalConfig config)
        {
            ModelState.Remove(nameof(AutoApprovalConfig.LastModifiedBy));
            ModelState.Remove(nameof(AutoApprovalConfig.Description));
            if (config.MaxAutoApprovalAmount < 0)
                ModelState.AddModelError(nameof(config.MaxAutoApprovalAmount), "The amount can't be negative.");
            if (!ModelState.IsValid)
                return View(config);

            var user = await _userManager.GetUserAsync(User);
            var modifiedBy = user?.Name ?? "Unknown";
            await _autoApprovalConfig.UpdateConfigAsync(config, modifiedBy);
            await _auditService.LogAuditAsync(null, "Auto-Approval Configuration", modifiedBy,
                $"Max amount R{config.MaxAutoApprovalAmount:N2}, enabled: {config.EnableAutoApproval}");

            TempData["SuccessMessage"] = "Auto-approval settings updated.";
            return RedirectToAction(nameof(Dashboard));
        }

        // ---------------------------------------------------------------- lecturers
        [HttpGet]
        public async Task<IActionResult> ManageLecturers()
        {
            return View(await _userManager.GetUsersInRoleAsync(LecturerRole));
        }

        [HttpGet]
        public async Task<IActionResult> EditLecturer(string id)
        {
            var lecturer = await FindLecturerAsync(id);
            if (lecturer == null)
                return NotFound();

            return View(new EditLecturerViewModel
            {
                Id = lecturer.Id,
                Email = lecturer.Email ?? string.Empty,
                FirstName = lecturer.FirstName ?? string.Empty,
                LastName = lecturer.LastName ?? string.Empty,
                Department = lecturer.Department ?? string.Empty,
                Specialization = lecturer.Specialization ?? string.Empty
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLecturer(EditLecturerViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var lecturer = await FindLecturerAsync(model.Id);
            if (lecturer == null)
                return NotFound();

            lecturer.FirstName = model.FirstName;
            lecturer.LastName = model.LastName;
            lecturer.Name = $"{model.FirstName} {model.LastName}";
            lecturer.Department = model.Department;
            lecturer.Specialization = model.Specialization;

            var result = await _userManager.UpdateAsync(lecturer);
            if (result.Succeeded && !string.Equals(lecturer.Email, model.Email, StringComparison.OrdinalIgnoreCase))
                result = await _userManager.SetEmailAsync(lecturer, model.Email);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            await AuditLecturerChangeAsync("Edit Lecturer", $"Updated details for {lecturer.Name}");
            TempData["SuccessMessage"] = "Lecturer details updated.";
            return RedirectToAction(nameof(ManageLecturers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLecturerStatus(string id)
        {
            var lecturer = await FindLecturerAsync(id);
            if (lecturer == null)
                return Json(new { success = false, message = "Lecturer not found." });

            lecturer.IsActive = !lecturer.IsActive;
            var result = await _userManager.UpdateAsync(lecturer);
            if (!result.Succeeded)
                return Json(new { success = false, message = "Failed to update the lecturer's status." });

            await AuditLecturerChangeAsync(lecturer.IsActive ? "Activate Lecturer" : "Deactivate Lecturer",
                $"{(lecturer.IsActive ? "Activated" : "Deactivated")} {lecturer.Name}");
            return Json(new { success = true });
        }

        /// <summary>Returns the user only if they are a lecturer, so HR can't edit other staff accounts.</summary>
        private async Task<ApplicationUser?> FindLecturerAsync(string? id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            var user = await _userManager.FindByIdAsync(id);
            return user != null && await _userManager.IsInRoleAsync(user, LecturerRole) ? user : null;
        }

        private async Task AuditLecturerChangeAsync(string action, string details)
        {
            var user = await _userManager.GetUserAsync(User);
            await _auditService.LogAuditAsync(null, action, user?.Name ?? "Unknown", details);
        }
    }
}
