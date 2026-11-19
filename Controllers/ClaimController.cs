using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CMCS.Models;
using CMCS.Models.Enums;
using CMCS.Models.ViewModels;
using CMCS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ClaimModel = CMCS.Models.Claim;

namespace CMCS.Controllers
{
    /// <summary>Claim submission for lecturers, plus rejection and document download for reviewers.</summary>
    [Authorize]
    public class ClaimController : Controller
    {
        private const string ReviewerRoles = "ProgrammeCoordinator,AcademicManager";

        private readonly IClaimsService _claimsService;
        private readonly ILogger<ClaimController> _logger;
        private readonly IDocumentService _documentService;
        private readonly IClaimApprovalService _approvalService;
        private readonly IClaimValidationService _validationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ClaimController(
            IClaimsService claimsService,
            ILogger<ClaimController> logger,
            IDocumentService documentService,
            IClaimApprovalService approvalService,
            IClaimValidationService validationService,
            UserManager<ApplicationUser> userManager)
        {
            _claimsService = claimsService;
            _logger = logger;
            _documentService = documentService;
            _approvalService = approvalService;
            _validationService = validationService;
            _userManager = userManager;
        }

        [Authorize(Roles = "Lecturer")]
        public IActionResult Submit()
        {
            return View(new ClaimModel { LecturerName = User.Identity?.Name ?? string.Empty });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> Submit(ClaimModel claim, IFormFile? supportingDocument)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            // Identity fields always come from the signed-in user, never from the form.
            claim.LecturerId = user.Id;
            claim.UserId = user.Id;
            claim.LecturerName = user.Name;
            claim.LecturerDepartment = string.IsNullOrWhiteSpace(claim.LecturerDepartment)
                ? user.Department ?? string.Empty
                : claim.LecturerDepartment;
            ModelState.Remove(nameof(ClaimModel.LecturerId));
            ModelState.Remove(nameof(ClaimModel.LecturerName));
            ModelState.Remove(nameof(ClaimModel.TotalAmount)); // recalculated on the server below

            var documentError = _documentService.Validate(supportingDocument);
            if (documentError != null)
                ModelState.AddModelError(nameof(ClaimModel.SupportingDocument), documentError);

            if (ModelState.IsValid)
            {
                var (isValid, messages) = await _validationService.ValidateClaimAsync(claim);
                foreach (var message in messages)
                    ModelState.AddModelError(string.Empty, message);
                if (!isValid)
                    return View(claim);
            }
            if (!ModelState.IsValid)
                return View(claim);

            try
            {
                claim.SubmissionDate = DateTime.Now;
                claim.TotalAmount = claim.HoursWorked * claim.HourlyRate;
                claim.SupportingDocumentPath = await _documentService.SaveDocumentAsync(supportingDocument!);
                claim.Status = await _approvalService.GetInitialStatusAsync(claim.TotalAmount);
                claim.RejectionReason = null;

                if (claim.Status == ClaimStatus.Approved)
                {
                    claim.DateReviewed = DateTime.Now;
                    claim.ReviewedBy = "System (auto-approval)";
                    claim.ApprovalComments = "Auto-approved: within the auto-approval limit";
                }
                else
                {
                    claim.ApprovalComments = "Pending review";
                }

                await _claimsService.CreateClaimAsync(claim);

                TempData["SuccessMessage"] = claim.Status == ClaimStatus.Approved
                    ? "Claim submitted and auto-approved."
                    : "Claim submitted and waiting for review.";
                return RedirectToAction(nameof(Dashboard));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting claim for lecturer {LecturerId}", user.Id);
                ModelState.AddModelError(string.Empty, "Something went wrong submitting your claim. Please try again.");
                return View(claim);
            }
        }

        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var claims = await _claimsService.GetAllClaimsAsync();
            var model = new DashboardViewModel
            {
                Claims = claims
                    .Where(c => c.LecturerId == userId)
                    .OrderByDescending(c => c.SubmissionDate)
                    .ToList()
            };

            ViewBag.SuccessMessage = TempData["SuccessMessage"];
            return View(model);
        }

        /// <summary>Shows the rejection form for a claim.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ReviewerRoles)]
        public async Task<IActionResult> InitiateReject(int id)
        {
            var claim = await _claimsService.GetClaimByIdAsync(id);
            return claim == null ? NotFound() : View("Reject", claim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ReviewerRoles)]
        public async Task<IActionResult> ConfirmReject(int id, string rejectionReason)
        {
            var reviewer = await _userManager.GetUserAsync(User);
            if (reviewer == null)
                return Challenge();

            var result = await _approvalService.RejectAsync(id, reviewer, rejectionReason);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToAction("Index", "Dashboard");
        }

        /// <summary>
        /// Downloads a claim's supporting document. Lecturers can only download their own;
        /// reviewers can download any.
        /// </summary>
        [Authorize(Roles = ReviewerRoles + ",Lecturer")]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var claim = await _claimsService.GetClaimByIdAsync(id);
            if (claim == null)
                return NotFound();

            var isReviewer = User.IsInRole("ProgrammeCoordinator") || User.IsInRole("AcademicManager");
            if (!isReviewer && claim.LecturerId != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return Forbid();

            var filePath = _documentService.GetDocumentPath(claim.SupportingDocumentPath);
            if (filePath == null)
                return NotFound();

            var downloadName = $"claim-{claim.Id}{Path.GetExtension(filePath)}";
            return PhysicalFile(filePath, ContentTypeFor(filePath), downloadName);
        }

        private static string ContentTypeFor(string path) => Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream"
        };
    }
}
