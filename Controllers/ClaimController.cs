using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CMCS.Models;
using CMCS.Models.Enums;
using CMCS.Models.ViewModels;
using System.Collections.Generic;
using System.IO;
using CMCS.Services;
using System.Security.Claims;
using ClaimModel = CMCS.Models.Claim;
using Microsoft.AspNetCore.Identity;
using CMCS.Data;
using Microsoft.EntityFrameworkCore;

namespace CMCS.Controllers
{
    /// <summary>
    /// Controller for handling claims.
    /// </summary>
    [Authorize]
    public class ClaimController : Controller
    {
        private readonly ILogger<ClaimController> _logger;
        private readonly IClaimsService _claimsService;
        private readonly IDocumentService _documentService;
        private readonly IClaimApprovalService _approvalService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ClaimController(
            IClaimsService claimsService,
            ILogger<ClaimController> logger,
            IDocumentService documentService,
            IClaimApprovalService approvalService,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _claimsService = claimsService;
            _logger = logger;
            _documentService = documentService;
            _approvalService = approvalService;
            _userManager = userManager;
            _context = context;
        }

        /// <summary>
        /// Displays the submit claim view.
        /// </summary>
        /// <returns>View result.</returns>
        [Authorize(Roles = "Lecturer")]
        [HttpGet]
        public async Task<IActionResult> Submit()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var claim = new ClaimModel
            {
                LecturerId = userId,
                LecturerName = user.FullName ?? "Unknown",
                SubmissionDate = DateTime.Now,
                AdditionalNotes = "",
                ModuleCode = ""
            };

            // Get active departments for the dropdown
            ViewBag.Departments = await _context.Departments
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name)
                .ToListAsync();

            return View(claim);
        }

        /// <summary>
        /// Handles the submit claim form submission.
        /// </summary>
        /// <param name="claim">Claim model.</param>
        /// <param name="supportingDocument">Supporting document file.</param>
        /// <returns>View result or redirect result.</returns>
        [Authorize(Roles = "Lecturer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(ClaimModel claim, IFormFile supportingDocument)
        {
            if (!ModelState.IsValid)
            {
                // Repopulate departments dropdown
                ViewBag.Departments = await _context.Departments
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.Name)
                    .ToListAsync();
                return View(claim);
            }

            try
            {
                // Validate department and module
                var department = await _context.Departments.FindAsync(claim.DepartmentId);
                var module = await _context.Modules.FindAsync(claim.ModuleId);

                if (department == null || !department.IsActive)
                {
                    ModelState.AddModelError("DepartmentId", "Please select a valid department");
                    ViewBag.Departments = await _context.Departments
                        .Where(d => d.IsActive)
                        .OrderBy(d => d.Name)
                        .ToListAsync();
                    return View(claim);
                }

                if (module == null || !module.IsActive || module.DepartmentId != claim.DepartmentId)
                {
                    ModelState.AddModelError("ModuleId", "Please select a valid module");
                    ViewBag.Departments = await _context.Departments
                        .Where(d => d.IsActive)
                        .OrderBy(d => d.Name)
                        .ToListAsync();
                    return View(claim);
                }

                // Set LecturerId if not already set
                if (string.IsNullOrEmpty(claim.LecturerId))
                {
                    claim.LecturerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                }

                // Validate file
                if (supportingDocument == null || supportingDocument.Length == 0)
                {
                    ModelState.AddModelError("SupportingDocument", "Supporting document is required");
                    ViewBag.Departments = await _context.Departments
                        .Where(d => d.IsActive)
                        .OrderBy(d => d.Name)
                        .ToListAsync();
                    return View(claim);
                }

                var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
                var fileExtension = Path.GetExtension(supportingDocument.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    TempData["ErrorMessage"] = "Please upload a valid document file (PDF, DOC, or DOCX)";
                    ViewBag.Departments = await _context.Departments
                        .Where(d => d.IsActive)
                        .OrderBy(d => d.Name)
                        .ToListAsync();
                    return View(claim);
                }

                if (supportingDocument.Length > 5 * 1024 * 1024) // 5MB limit
                {
                    TempData["ErrorMessage"] = "File size cannot exceed 5MB";
                    ViewBag.Departments = await _context.Departments
                        .Where(d => d.IsActive)
                        .OrderBy(d => d.Name)
                        .ToListAsync();
                    return View(claim);
                }

                // Calculate total amount
                decimal calculatedTotal = claim.HoursWorked * claim.HourlyRate;
                
                // Validate total amount
                if (calculatedTotal <= 0 || calculatedTotal > 3900000)
                {
                    ModelState.AddModelError("TotalAmount", "Total amount must be between R0.01 and R3,900,000");
                    ViewBag.Departments = await _context.Departments
                        .Where(d => d.IsActive)
                        .OrderBy(d => d.Name)
                        .ToListAsync();
                    return View(claim);
                }

                // Auto-approve claims under R5000
                if (calculatedTotal <= 5000)
                {
                    claim.Status = ClaimStatus.Approved;
                    claim.DateReviewed = DateTime.Now;
                    claim.ReviewedBy = "System";
                    claim.ApprovalComments = "Auto-approved: Amount less than or equal to R5000";
                }
                else
                {
                    claim.Status = ClaimStatus.Pending;
                }

                // Save the supporting document
                string uniqueFileName = $"{Guid.NewGuid()}_{supportingDocument.FileName}";
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await supportingDocument.CopyToAsync(fileStream);
                }

                claim.SupportingDocumentPath = Path.Combine("uploads", uniqueFileName).Replace("\\", "/");

                // Set submission date and user ID
                claim.SubmissionDate = DateTime.Now;
                claim.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Create the claim
                var createdClaim = await _claimsService.CreateClaimAsync(claim);

                if (createdClaim != null)
                {
                    TempData["SuccessMessage"] = claim.Status == ClaimStatus.Approved
                        ? "Claim submitted and auto-approved successfully!"
                        : "Claim submitted successfully and pending review.";
                    
                    return RedirectToAction(nameof(Dashboard));
                }

                TempData["ErrorMessage"] = "An error occurred while submitting your claim. Please try again.";
                ViewBag.Departments = await _context.Departments
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.Name)
                    .ToListAsync();
                return View(claim);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting claim for lecturer {LecturerName}", claim.LecturerName);
                TempData["ErrorMessage"] = "An error occurred while submitting your claim. Please try again.";
                ViewBag.Departments = await _context.Departments
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.Name)
                    .ToListAsync();
                return View(claim);
            }
        }

        /// <summary>
        /// Gets modules for a specific department.
        /// </summary>
        /// <param name="departmentId">Department ID.</param>
        /// <returns>JSON result containing modules.</returns>
        [HttpGet]
        [Route("api/[controller]/[action]/{departmentId}")]
        [Authorize(Roles = "Lecturer")]
        public async Task<JsonResult> GetModules(int departmentId)
        {
            try
            {
                var modules = await _context.Modules
                    .Where(m => m.DepartmentId == departmentId && m.IsActive)
                    .OrderBy(m => m.Name)
                    .Select(m => new { id = m.Id, name = m.Name })
                    .ToListAsync();

                return Json(modules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting modules for department {DepartmentId}", departmentId);
                return Json(new { error = "Error loading modules" });
            }
        }

        /// <summary>
        /// Controller for managing the dashboard functionality for lecturers.
        /// </summary>
        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> Dashboard()
        {
            var claims = await _claimsService.GetAllClaimsAsync();
            var model = new DashboardViewModel
            {
                Claim = new ClaimModel(),
                Claims = claims.Where(c => c.LecturerName == User.Identity.Name).ToList(),
                PendingClaims = claims.Where(c => c.Status == ClaimStatus.Pending).ToList()
            };

            ViewBag.SuccessMessage = TempData["SuccessMessage"];
            return View(model);
        }

        /// <summary>
        /// Approves a claim by updating its status.
        /// </summary>
        /// <param name="id">The ID of the claim to approve.</param>
        /// <returns>A redirect to the claims list.</returns>
        [Authorize(Roles = "ProgrammeCoordinator,AcademicManager")]
        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var claim = await _claimsService.GetClaimByIdAsync(id);
            if (claim == null)
            {
                return NotFound("Claim not found.");
            }

            claim.Status = ClaimStatus.Approved;
            await _claimsService.UpdateClaimAsync(claim);
            _logger.LogInformation($"Claim ID {id} approved.");

            TempData["SuccessMessage"] = $"Claim ID {id} approved successfully.";
            return RedirectToAction("Index", "Dashboard");
        }

        /// <summary>
        /// Shows the rejection page for a claim.
        /// </summary>
        [Authorize(Roles = "ProgrammeCoordinator,AcademicManager")]
        [HttpPost]
        public async Task<IActionResult> InitiateReject(int id)
        {
            var claim = await _claimsService.GetClaimByIdAsync(id);
            if (claim == null)
            {
                return NotFound("Claim not found.");
            }

            return View("Reject", claim);
        }

        /// <summary>
        /// Handles the confirmation of claim rejection.
        /// </summary>
        [Authorize(Roles = "ProgrammeCoordinator,AcademicManager")]
        [HttpPost]
        public async Task<IActionResult> ConfirmReject(int id, string rejectionReason)
        {
            if (string.IsNullOrWhiteSpace(rejectionReason))
            {
                TempData["ErrorMessage"] = "A rejection reason is required.";
                return RedirectToAction("InitiateReject", new { id });
            }

            var claim = await _claimsService.GetClaimByIdAsync(id);
            if (claim == null)
            {
                return NotFound("Claim not found.");
            }

            claim.Status = ClaimStatus.Rejected;
            claim.RejectionReason = rejectionReason;
            await _claimsService.UpdateClaimAsync(claim);

            _logger.LogInformation($"Claim ID {id} rejected with reason: {rejectionReason}");
            TempData["SuccessMessage"] = "Claim rejected successfully.";
            return RedirectToAction("Index", "Dashboard");
        }

        /// <summary>
        /// Controller for listing all claims.
        /// </summary>
        /// <returns>The list of claims.</returns>
        [Authorize(Roles = "ProgrammeCoordinator,AcademicManager")]
        public async Task<IActionResult> ListClaims()
        {
            ViewBag.SuccessMessage = TempData["SuccessMessage"];
            var claims = await _claimsService.GetAllClaimsAsync();
            return View(claims);
        }

        /// <summary>
        /// Controller for downloading a document associated with a claim.
        /// </summary>
        /// <param name="id">The ID of the claim.</param>
        /// <returns>The downloaded document or a "Not Found" response if the document is not found.</returns>
        [Authorize(Roles = "ProgrammeCoordinator,AcademicManager,Lecturer")]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var claim = await _claimsService.GetClaimByIdAsync(id);
            if (claim == null || string.IsNullOrEmpty(claim.SupportingDocumentPath))
            {
                return NotFound("Document not found.");
            }

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", claim.SupportingDocumentPath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("File not found.");
            }

            var fileName = Path.GetFileName(filePath);
            var mimeType = "application/octet-stream";

            return PhysicalFile(filePath, mimeType, fileName);
        }

        private ClaimStatus GetClaimStatusFromString(string status)
        {
            return status switch
            {
                "Pending" => ClaimStatus.Pending,
                "Approved" => ClaimStatus.Approved,
                "Rejected" => ClaimStatus.Rejected,
                _ => ClaimStatus.Pending
            };
        }
    }
}
