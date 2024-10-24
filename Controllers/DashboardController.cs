using CMCS.Models;
using CMCS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
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

            if (User.IsInRole("Lecturer"))
            {
                viewModel.Claims = _claimsService.GetClaims()
                    .Where(c => c.LecturerName == User.Identity.Name)
                    .OrderByDescending(c => c.DateSubmitted)
                    .ToList();
            }
            else if (User.IsInRole("ProgrammeCoordinator") || User.IsInRole("AcademicManager"))
            {
                viewModel.PendingClaims = _claimsService.GetPendingClaims()
                    .OrderByDescending(c => c.DateSubmitted)
                    .ToList();
                viewModel.ManagedClaims = _claimsService.GetManagedClaims()
                    .OrderByDescending(c => c.DateSubmitted)
                    .ToList();
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ProgrammeCoordinator,AcademicManager")]
        public IActionResult UpdateClaimStatus(int id, string status)
        {
            try
            {
                var claim = _claimsService.GetClaimById(id);
                if (claim != null)
                {
                    _claimsService.UpdateClaimStatus(id, status);
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