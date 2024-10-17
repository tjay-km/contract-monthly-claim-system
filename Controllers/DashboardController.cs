using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CMCS.Models;
using System.Collections.Generic;
using System.Linq;

namespace CMCS.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly List<Claim> _claims;

        public DashboardController(List<Claim> claims)
        {
            _claims = claims;
        }

        public IActionResult Index()
        {
            if (User.IsInRole("Lecturer"))
            {
                var lecturerClaims = _claims.Where(c => c.LecturerName == User.Identity.Name).ToList();
                return View("LecturerDashboard", lecturerClaims);
            }
            else if (User.IsInRole("ProgrammeCoordinator") || User.IsInRole("AcademicManager"))
            {
                return View("ManagerDashboard", _claims);
            }
            return View("Index");
        }

        [HttpPost]
        [Authorize(Roles = "ProgrammeCoordinator,AcademicManager")]
        public IActionResult UpdateClaimStatus(int id, string status)
        {
            var claim = _claims.FirstOrDefault(c => c.Id == id);
            if (claim != null)
            {
                claim.Status = status;
                return Json(new { success = true, message = $"Claim {id} status updated to {status}" });
            }
            return Json(new { success = false, message = "Claim not found" });
        }
    }
}