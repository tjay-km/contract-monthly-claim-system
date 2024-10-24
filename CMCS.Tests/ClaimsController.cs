using CMCS.Services;
using Microsoft.AspNetCore.Mvc;

namespace CMCS.Controllers
{
    public class ClaimsController : Controller
    {
        private readonly IClaimsService _claimService;

        public ClaimsController(IClaimsService claimService)
        {
            _claimService = claimService;
        }

        [HttpPost]
        public IActionResult UpdateClaimStatus(int id, string status)
        {
            // Validate input
            if (id <= 0 || string.IsNullOrWhiteSpace(status))
            {
                return BadRequest(new { success = false, message = "Invalid input." });
            }

            // Update status
            var claim = _claimService.GetClaimById(id);
            if (claim == null)
            {
                return Json(new { success = false, message = "Error updating claim status." });
            }

            _claimService.UpdateClaimStatus(id, status);

            return Json(new { success = true, message = "Claim status updated successfully." });
        }
    }
}
