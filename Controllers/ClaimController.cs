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
    [Authorize]
    public class ClaimController : Controller
    {
        private readonly List<Claim> _claims;
        private readonly ILogger<ClaimController> _logger;

        public ClaimController(List<Claim> claims, ILogger<ClaimController> logger)
        {
            _claims = claims ?? new List<Claim>();
            _logger = logger;
        }

        [Authorize(Roles = "Lecturer")]
        public IActionResult Submit()
        {
            return View();
        }

        [Authorize(Roles = "Lecturer")]
        [HttpPost]
        public async Task<IActionResult> Submit(Claim claim, IFormFile? supportingDocument) // Allow supportingDocument to be null
        {
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogError(error.ErrorMessage);
                }
                return View(claim);
            }

            // Check if the file is present
            if (supportingDocument != null && supportingDocument.Length > 0) // Only save if a file is uploaded
            {
                claim.SupportingDocumentPath = await SaveSupportingDocument(supportingDocument);
            }
            else
            {
                claim.SupportingDocumentPath = null; // or leave it unset
            }

            try
            {
                claim.LecturerName = User.Identity.Name;
                claim.DateSubmitted = DateTime.Now;
                claim.Status = "Pending";

                claim.Id = _claims.Count + 1;
                _claims.Add(claim);

                _logger.LogInformation($"Claim submitted by {claim.LecturerName} on {claim.DateSubmitted}.");

                TempData["SuccessMessage"] = "Claim submitted successfully!";
                return RedirectToAction("Dashboard"); // Redirect to the dashboard view
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting claim");
                ModelState.AddModelError("", "An error occurred while submitting the claim. Please try again.");
            }

            return View(claim);
        }

        private async Task<string> SaveSupportingDocument(IFormFile file)
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

            return $"/uploads/{fileName}";
        }

        [Authorize(Roles = "Lecturer")]
        public IActionResult Dashboard()
        {
            var model = new DashboardViewModel
            {
                Claim = new Claim(), // Reset the claim for a new submission
                Claims = _claims.Where(c => c.LecturerName == User.Identity.Name).ToList(),
                PendingClaims = _claims.Where(c => c.Status == "Pending").ToList()
            };

            // Display success message if it exists
            ViewBag.SuccessMessage = TempData["SuccessMessage"];
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
                return NotFound("Document not found.");
            }

            // Build the full file path from the stored document path
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", claim.SupportingDocumentPath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("File not found.");
            }

            var fileName = Path.GetFileName(filePath);
            var mimeType = "application/octet-stream"; // Default MIME type

            return PhysicalFile(filePath, mimeType, fileName);
        }

        [Authorize(Roles = "ProgrammeCoordinator,AcademicManager")]
        public IActionResult ListClaims()
        {
            return View(_claims); // Return all claims stored in memory
        }
    }
}
