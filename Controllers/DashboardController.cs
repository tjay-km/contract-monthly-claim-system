using CMCS.Models;
using CMCS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task<IActionResult> Index()
        {
            var viewModel = new DashboardViewModel();

            if (User.IsInRole("Lecturer"))
            {
                var claims = await _claimsService.GetAllClaimsAsync();
                viewModel.Claims = claims
                    .Where(c => c.LecturerName == User.Identity.Name)
                    .OrderByDescending(c => c.DateSubmitted)
                    .ToList();
            }
            else if (User.IsInRole("ProgrammeCoordinator") || User.IsInRole("AcademicManager"))
            {
                var allClaims = await _claimsService.GetAllClaimsAsync();
                viewModel.PendingClaims = allClaims
                    .Where(c => c.Status == "Pending")
                    .OrderByDescending(c => c.DateSubmitted)
                    .ToList();
                viewModel.ManagedClaims = allClaims
                    .Where(c => c.Status == "Approved" || c.Status == "Rejected")
                    .OrderByDescending(c => c.DateSubmitted)
                    .ToList();
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ProgrammeCoordinator,AcademicManager")]
        public async Task<IActionResult> UpdateClaimStatus(int id, string status)
        {
            try
            {
                // Only allow approval through this endpoint
                if (status != "Approved")
                {
                    return Json(new { success = false, message = "This endpoint only supports claim approval. For rejections, please use the dedicated rejection flow." });
                }

                var claim = await _claimsService.GetClaimByIdAsync(id);
                if (claim != null)
                {
                    claim.Status = status;
                    await _claimsService.UpdateClaimAsync(claim);
                    return Json(new { success = true, message = $"Claim {id} status updated to {status}" });
                }
                return Json(new { success = false, message = "Claim not found" });
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, new { success = false, message = "An error occurred while processing your request." });
            }
        }
    }
}