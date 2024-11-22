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

        public ClaimController(
            IClaimsService claimsService,
            ILogger<ClaimController> logger,
            IDocumentService documentService,
            IClaimApprovalService approvalService,
            UserManager<ApplicationUser> userManager)
        {
            _claimsService = claimsService;
            _logger = logger;
            _documentService = documentService;
            _approvalService = approvalService;
            _userManager = userManager;
        }

        /// <summary>
        /// Displays the submit claim view.
        /// </summary>
        /// <returns>View result.</returns>
        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> Submit()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var claim = new ClaimModel
            {
                LecturerId = userId,
                LecturerName = User.Identity?.Name ?? string.Empty
            };
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
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        TempData["ErrorMessage"] = error.ErrorMessage;
                    }
                }
                return View(claim);
            }

            try
            {
                // Set LecturerId if not already set
                if (string.IsNullOrEmpty(claim.LecturerId))
                {
                    claim.LecturerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                }

                // Validate file
                if (supportingDocument == null || supportingDocument.Length == 0)
                {
                    ModelState.AddModelError("SupportingDocument", "Supporting document is required");
                    return View(claim);
                }

                var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
                var fileExtension = Path.GetExtension(supportingDocument.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    TempData["ErrorMessage"] = "Please upload a valid document file (PDF, DOC, or DOCX)";
                    return View(claim);
                }

                if (supportingDocument.Length > 5 * 1024 * 1024) // 5MB limit
                {
                    TempData["ErrorMessage"] = "File size cannot exceed 5MB";
                    return View(claim);
                }

                // Calculate total amount
                decimal calculatedTotal = claim.HoursWorked * claim.HourlyRate;
                
                // Validate total amount
                if (calculatedTotal <= 0 || calculatedTotal > 3900000)
                {
                    ModelState.AddModelError("TotalAmount", "Total amount must be between R0.01 and R3,900,000");
                    return View(claim);
                }

                // Set the calculated total
                claim.TotalAmount = calculatedTotal;

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
                if (supportingDocument != null && supportingDocument.Length > 0)
                {
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
                }

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
                return View(claim);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting claim for lecturer {LecturerName}", claim.LecturerName);
                TempData["ErrorMessage"] = "An error occurred while submitting your claim. Please try again.";
                return View(claim);
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
    }
}
