using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CMCS.Models;
using CMCS.Models.Enums;
using CMCS.Models.ViewModels;
using CMCS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CMCS.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IClaimsService _claimsService;
        private readonly IClaimApprovalService _approvalService;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(
            IClaimsService claimsService,
            IClaimApprovalService approvalService,
            UserManager<ApplicationUser> userManager)
        {
            _claimsService = claimsService;
            _approvalService = approvalService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new DashboardViewModel();
            var claims = await _claimsService.GetAllClaimsAsync();

            if (User.IsInRole("Lecturer"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                viewModel.Claims = claims
                    .Where(c => c.LecturerId == userId)
                    .OrderByDescending(c => c.SubmissionDate)
                    .ToList();
            }
            else if (User.IsInRole("ProgrammeCoordinator") || User.IsInRole("AcademicManager"))
            {
                viewModel.PendingClaims = claims
                    .Where(c => ClaimApprovalService.IsPending(c.Status))
                    .OrderByDescending(c => c.SubmissionDate)
                    .ToList();
                viewModel.ManagedClaims = claims
                    .Where(c => c.Status is ClaimStatus.Approved or ClaimStatus.Rejected)
                    .OrderByDescending(c => c.SubmissionDate)
                    .ToList();
            }

            return View(viewModel);
        }

        /// <summary>Approves a claim (called from the dashboard). Rejections use the rejection form.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ProgrammeCoordinator,AcademicManager")]
        public async Task<IActionResult> UpdateClaimStatus(int id, ClaimStatus status)
        {
            if (status != ClaimStatus.Approved)
                return Json(new { success = false, message = "Use the rejection form to reject a claim." });

            var reviewer = await _userManager.GetUserAsync(User);
            if (reviewer == null)
                return Challenge();

            var result = await _approvalService.ApproveAsync(id, reviewer, User.IsInRole("AcademicManager"));
            return Json(new { success = result.Succeeded, message = result.Message });
        }
    }
}
