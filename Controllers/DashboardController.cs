using CMCS.Models;
using CMCS.Services; // Ensure this using directive is included
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace CMCS.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IClaimsService _claimsService;

        public DashboardController(IClaimsService claimsService)
        {
            _claimsService = claimsService;
        }

        public IActionResult Index()
        {
            var viewModel = new DashboardViewModel();

            var claims = _claimsService.GetClaims(); // Get claims from the service

            if (User.IsInRole("Lecturer"))
            {
                viewModel.Claims = claims
                    .Where(c => c.LecturerName == User.Identity.Name)
                    .OrderByDescending(c => c.DateSubmitted)
                    .ToList();
            }
            else if (User.IsInRole("ProgrammeCoordinator") || User.IsInRole("AcademicManager"))
            {
                viewModel.PendingClaims = claims
                    .Where(c => c.Status == "Pending")
                    .OrderByDescending(c => c.DateSubmitted)
                    .ToList();
            }

            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "ProgrammeCoordinator,AcademicManager")]
        public IActionResult UpdateClaimStatus(int id, string status)
        {
            var claim = _claimsService.GetClaimById(id);
            if (claim != null)
            {
                _claimsService.UpdateClaimStatus(id, status);
                return Json(new { success = true, message = $"Claim {id} status updated to {status}" });
            }
            return Json(new { success = false, message = "Claim not found" });
        }
    }
}
