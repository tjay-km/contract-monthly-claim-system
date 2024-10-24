using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CMCS.Models;
using System.Collections.Generic;
using System.IO;

namespace CMCS.Controllers
{
    /// <summary>
    /// Controller for handling claims.
    /// </summary>
    [Authorize]
    public class ClaimController : Controller
    {
        private readonly List<Claim> _claims; // List of claims
        private readonly ILogger<ClaimController> _logger; // Logger instance

        /// <summary>
        /// Initializes a new instance of the <see cref="ClaimController"/> class.
        /// </summary>
        /// <param name="claims">List of claims.</param>
        /// <param name="logger">Logger instance.</param>
        public ClaimController(List<Claim> claims, ILogger<ClaimController> logger)
        {
            _claims = claims ?? new List<Claim>(); // Initialize claims list if null
            _logger = logger; // Initialize logger instance
        }

        /// <summary>
        /// Displays the submit claim view.
        /// </summary>
        /// <returns>View result.</returns>
        [Authorize(Roles = "Lecturer")]
        public IActionResult Submit()
        {
            return View();
        }

        /// <summary>
        /// Handles the submit claim form submission.
        /// </summary>
        /// <param name="claim">Claim model.</param>
        /// <param name="supportingDocument">Supporting document file.</param>
        /// <returns>View result or redirect result.</returns>
        [Authorize(Roles = "Lecturer")]
        [HttpPost]
        public async Task<IActionResult> Submit(Claim claim, IFormFile? supportingDocument) // Allow supportingDocument to be null
        {
            // Check if model state is valid
            if (!ModelState.IsValid)
            {
                // Log model state errors
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogError(error.ErrorMessage);
                }
                return View(claim); // Return view with errors
            }

            // Check if supporting document is present
            if (supportingDocument != null && supportingDocument.Length > 0) // Only save if a file is uploaded
            {
                // Save supporting document
                claim.SupportingDocumentPath = await SaveSupportingDocument(supportingDocument);
            }
            else
            {
                claim.SupportingDocumentPath = null; // or leave it unset
            }

            try
            {
                // Set claim properties
                claim.LecturerName = User.Identity.Name;
                claim.DateSubmitted = DateTime.Now;
                claim.Status = "Pending";

                // Add claim to list
                claim.Id = _claims.Count + 1;
                _claims.Add(claim);

                // Log claim submission
                _logger.LogInformation($"Claim submitted by {claim.LecturerName} on {claim.DateSubmitted}.");

                // Set success message
                TempData["SuccessMessage"] = "Claim submitted successfully!";

                // Redirect to dashboard view
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                // Log exception
                _logger.LogError(ex, "Error submitting claim");

                // Add model state error
                ModelState.AddModelError("", "An error occurred while submitting the claim. Please try again.");
            }

            // Return view with errors
            return View(claim);
        }

        /// <summary>
        /// Saves the supporting document to the uploads directory.
        /// </summary>
        /// <param name="file">Supporting document file.</param>
        /// <returns>Path to saved document.</returns>
        private async Task<string> SaveSupportingDocument(IFormFile file)
        {
            // Get upload path
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            // Create upload directory if it doesn't exist
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            // Generate file name
            var fileName = Path.GetRandomFileName() + Path.GetExtension(file.FileName);

            // Get file path
            var filePath = Path.Combine(uploadPath, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return path to saved document
            return $"/uploads/{fileName}";
        }

        /// <summary>
        /// Controller for managing the dashboard functionality for lecturers.
        /// </summary>
        [Authorize(Roles = "Lecturer")]
        public IActionResult Dashboard()
        {
            var model = new DashboardViewModel
            {
                Claim = new Claim(),
                Claims = _claims.Where(c => c.LecturerName == User.Identity.Name).ToList(),
                PendingClaims = _claims.Where(c => c.Status == "Pending").ToList()
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
        public IActionResult Approve(int id)
        {
            var claim = _claims.FirstOrDefault(c => c.Id == id);
            if (claim == null)
            {
                return NotFound("Claim not found.");
            }

            claim.Status = "Approved"; // Update claim status
            _logger.LogInformation($"Claim ID {id} approved.");

            TempData["SuccessMessage"] = $"Claim ID {id} approved successfully."; // Success message
            return RedirectToAction("ListClaims"); // Redirect to the claims list
        }

        /// <summary>
        /// Rejects a claim by updating its status.
        /// </summary>
        /// <param name="id">The ID of the claim to reject.</param>
        /// <returns>A redirect to the claims list.</returns>
        [Authorize(Roles = "ProgrammeCoordinator,AcademicManager")]
        [HttpPost]
        public IActionResult Reject(int id)
        {
            var claim = _claims.FirstOrDefault(c => c.Id == id);
            if (claim == null)
            {
                return NotFound("Claim not found.");
            }

            claim.Status = "Rejected"; // Update claim status
            _logger.LogInformation($"Claim ID {id} rejected.");

            TempData["SuccessMessage"] = $"Claim ID {id} rejected successfully."; // Success message
            return RedirectToAction("ListClaims"); // Redirect to the claims list
        }

        /// <summary>
        /// Controller for listing all claims.
        /// </summary>
        /// <returns>The list of claims.</returns>
        [Authorize(Roles = "ProgrammeCoordinator,AcademicManager")]
        public IActionResult ListClaims()
        {
            ViewBag.SuccessMessage = TempData["SuccessMessage"]; // Display success message if exists
            return View(_claims); // Return all claims stored in memory
        }

        /// <summary>
        /// Controller for downloading a document associated with a claim.
        /// </summary>
        /// <param name="id">The ID of the claim.</param>
        /// <returns>The downloaded document or a "Not Found" response if the document is not found.</returns>
        [Authorize(Roles = "ProgrammeCoordinator,AcademicManager,Lecturer")]
        public IActionResult DownloadDocument(int id)
        {
            var claim = _claims.FirstOrDefault(c => c.Id == id);
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
            var mimeType = "application/octet-stream"; // Default MIME type

            return PhysicalFile(filePath, mimeType, fileName);
        }
    }
}
