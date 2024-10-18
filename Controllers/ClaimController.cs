using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CMCS.Models;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CMCS.Controllers
{
    [Authorize]
    public class ClaimController : Controller
    {
        private readonly List<Claim> _claims; // In-memory storage for claims
        private readonly ILogger<ClaimController> _logger;

        // Inject the in-memory claims list and logger via constructor
        public ClaimController(List<Claim> claims, ILogger<ClaimController> logger)
        {
            _claims = claims ?? new List<Claim>(); // Initialize if null
            _logger = logger;
        }

        [Authorize(Roles = "Lecturer")]
        public IActionResult Submit()
        {
            return View();
        }

        [Authorize(Roles = "Lecturer")]
        [HttpPost]
        public async Task<IActionResult> Submit(Claim claim, IFormFile supportingDocument)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Set claim details
                    claim.LecturerName = User.Identity.Name;
                    claim.DateSubmitted = DateTime.Now;
                    claim.Status = "Pending";

                    // Handle file upload
                    if (supportingDocument != null && supportingDocument.Length > 0)
                    {
                        claim.SupportingDocumentPath = await SaveSupportingDocument(supportingDocument);
                    }

                    // Add claim to the in-memory list
                    claim.Id = _claims.Count + 1; // Simulate auto-increment
                    _claims.Add(claim); // Store claim in memory

                    _logger.LogInformation($"Claim submitted by {claim.LecturerName} on {claim.DateSubmitted}.");

                    // Redirect to the Success view after successful submission
                    return RedirectToAction("Success");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error submitting claim");
                    ModelState.AddModelError("", "An error occurred while submitting the claim. Please try again.");
                }
            }

            return View(claim); // Return the view with the model if the model state is invalid
        }



        private async Task<string> SaveSupportingDocument(IFormFile supportingDocument)
        {
            var fileName = Path.GetFileName(supportingDocument.FileName);
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", fileName);

            // Ensure the uploads directory exists
            if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads")))
            {
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads"));
            }

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await supportingDocument.CopyToAsync(stream);
            }

            return "/uploads/" + fileName;
        }

        // New action for the success page
        public IActionResult Success()
        {
            ViewBag.SuccessMessage = "Claim submitted successfully!";
            var model = new DashboardViewModel
            {
                Claim = new Claim(), // Reset the claim for a new submission
                Claims = _claims.Where(c => c.LecturerName == User.Identity.Name).ToList(),
                PendingClaims = _claims.Where(c => c.Status == "Pending").ToList()
            };
            return View(model);
        }

        [Authorize(Roles = "ProgrammeCoordinator,AcademicManager")]
        [HttpPost]
        public IActionResult UpdateClaimStatus(int id, string status)
        {
            var claim = _claims.FirstOrDefault(c => c.Id == id);
            if (claim == null)
            {
                return Json(new { success = false, message = "Claim not found" });
            }

            // Update claim status
            claim.Status = status;

            _logger.LogInformation($"Claim ID {id} status updated to {status}.");
            return Json(new { success = true, message = $"Claim status updated to {status}" });
        }

        [Authorize(Roles = "ProgrammeCoordinator,AcademicManager,Lecturer")]
        public IActionResult DownloadDocument(int id)
        {
            var claim = _claims.FirstOrDefault(c => c.Id == id);
            if (claim == null || string.IsNullOrEmpty(claim.SupportingDocumentPath))
            {
                return NotFound();
            }

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", claim.SupportingDocumentPath.TrimStart('/'));
            var fileName = Path.GetFileName(filePath);
            var mimeType = "application/octet-stream";

            return PhysicalFile(filePath, mimeType, fileName);
        }

        [Authorize(Roles = "ProgrammeCoordinator,AcademicManager")]
        public IActionResult ListClaims()
        {
            return View(_claims); // Return all claims stored in memory
        }
    }
}
