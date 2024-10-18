using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMCS.Tests
{
    public class ClaimsController : Controller
    {
        private readonly IClaimService _claimService;

        public ClaimsController(IClaimService claimService)
        {
            _claimService = claimService;
        }

        [HttpPost]
        public IActionResult UpdateClaimStatus(int id, string status)
        {
            var result = _claimService.UpdateStatus(id, status);
            if (result)
            {
                return Json(new { success = true, message = "Claim status updated successfully." });
            }
            return Json(new { success = false, message = "Error updating claim status." });
        }
    }

}
