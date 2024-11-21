using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CMCS.Models;
using System.Collections.Generic;
using System.IO;
using CMCS.Services;
using System.Security.Claims;
using ClaimModel = CMCS.Models.Claim;

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

        public ClaimController(
            IClaimsService claimsService,
            ILogger<ClaimController> logger,
            IDocumentService documentService)
        {
            _claimsService = claimsService;
            _logger = logger;
            _documentService = documentService;
        }

        /// <summary>
        /// Displays the submit claim view.
        /// </summary>
        /// <returns>View result.</returns>
        [Authorize(Roles = "Lecturer")]
        public IActionResult Submit()
        {
            return View(new ClaimModel());
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
                return View(claim);
            }

            try
            {
                // Validate file
                if (supportingDocument == null || supportingDocument.Length == 0)
                {
                    ModelState.AddModelError("SupportingDocument", "Please upload a supporting document");
                    return View(claim);
                }

                var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
                var fileExtension = Path.GetExtension(supportingDocument.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("SupportingDocument", "Please upload a valid document file (PDF, DOC, or DOCX)");
                    return View(claim);
                }

                if (supportingDocument.Length > 5 * 1024 * 1024) // 5MB limit
                {
                    ModelState.AddModelError("SupportingDocument", "File size cannot exceed 5MB");
                    return View(claim);
                }

                // Set additional claim properties
                claim.DateSubmitted = DateTime.Now;
                claim.Status = "Pending";
                claim.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Save claim first to get the ID
                await _claimsService.CreateClaimAsync(claim);

                // Now save the document with the claim ID
                var documentPath = await SaveSupportingDocument(supportingDocument, claim.Id);
                
                // Update claim with document path
                claim.SupportingDocumentPath = documentPath;
                await _claimsService.UpdateClaimAsync(claim);

                _logger.LogInformation("Claim submitted successfully for lecturer {LecturerName}", claim.LecturerName);
                TempData["SuccessMessage"] = "Claim submitted successfully!";
                
                return RedirectToAction(nameof(Dashboard));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting claim for lecturer {LecturerName}", claim.LecturerName);
                ModelState.AddModelError("", "An error occurred while submitting your claim. Please try again.");
                return View(claim);
            }
        }

        /// <summary>
        /// Saves the supporting document to the uploads directory.
        /// </summary>
        /// <param name="file">Supporting document file.</param>
        /// <param name="claimId">The ID of the claim.</param>
        /// <returns>Path to saved document.</returns>
        private async Task<string> SaveSupportingDocument(IFormFile file, int claimId)
        {
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            var fileName = Path.GetRandomFileName() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = $"/uploads/{fileName}";

            // Create and save document metadata
            var claimDocument = new CMCS.Models.ClaimDocument
            {
                ClaimId = claimId,
                FileName = file.FileName,
                FilePath = relativePath,
                DateUploaded = DateTime.Now
            };

            await _documentService.AddClaimDocument(claimDocument);

            return relativePath;
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
                PendingClaims = claims.Where(c => c.Status == "Pending").ToList()
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

            claim.Status = "Approved";
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

            claim.Status = "Rejected";
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
