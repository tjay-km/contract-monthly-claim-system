using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CMCS.Models;
using CMCS.Services; // Add this using directive
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
            var claims = _claimsService.GetClaims();
            if (User.IsInRole("Lecturer"))
            {
                var lecturerClaims = claims.Where(c => c.LecturerName == User.Identity.Name).ToList();
                return View("LecturerDashboard", lecturerClaims);
            }
            else if (User.IsInRole("ProgrammeCoordinator") || User.IsInRole("AcademicManager"))
            {
                return View("ManagerDashboard", claims);
            }
            return View("Index");
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
