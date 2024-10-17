//using Microsoft.AspNetCore.Mvc;
//using System.Collections.Generic;
//using System.Linq;
//using CMCS.Models;

//public class ClaimController : Controller
//{
//    private static List<Claim> _claims = new List<Claim>();

//    public IActionResult Index()
//    {
//        return View(_claims);
//    }

//    [HttpGet]
//    public IActionResult Submit()
//    {
//        return View();
//    }

//    [HttpPost]
//    public IActionResult Submit(Claim claim, IFormFile supportingDocument)
//    {
//        if (ModelState.IsValid)
//        {
//            claim.Id = _claims.Count + 1;
//            claim.Status = "Pending";

//            if (supportingDocument != null)
//            {
//                // Logic to save the file and set the path
//                var filePath = Path.Combine("uploads", supportingDocument.FileName);
//                using (var stream = new FileStream(filePath, FileMode.Create))
//                {
//                    supportingDocument.CopyTo(stream);
//                }
//                claim.SupportingDocumentPath = filePath;
//            }

//            _claims.Add(claim);
//            return RedirectToAction("Index");
//        }
//        return View(claim);
//    }

//    [HttpPost]
//    public IActionResult Approve(int id)
//    {
//        var claim = _claims.FirstOrDefault(c => c.Id == id);
//        if (claim != null)
//        {
//            claim.Status = "Approved";
//        }
//        return RedirectToAction("Index");
//    }

//    [HttpPost]
//    public IActionResult Reject(int id)
//    {
//        var claim = _claims.FirstOrDefault(c => c.Id == id);
//        if (claim != null)
//        {
//            claim.Status = "Rejected";
//        }
//        return RedirectToAction("Index");
//    }
//}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CMCS.Models;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace CMCS.Controllers
{
    [Authorize]
    public class ClaimController : Controller
    {
        private readonly List<Claim> _claims;

        public ClaimController(List<Claim> claims)
        {
            _claims = claims;
        }

        [Authorize(Policy = "Lecturer")]
        public IActionResult Submit()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Policy = "Lecturer")]
        public IActionResult Submit(Claim claim, IFormFile supportingDocument)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    claim.Id = _claims.Count + 1;
                    claim.Status = "Pending";
                    claim.LecturerName = User.Identity.Name;

                    if (supportingDocument != null)
                    {
                        var fileName = Path.GetFileName(supportingDocument.FileName);
                        var filePath = Path.Combine("uploads", fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            supportingDocument.CopyTo(stream);
                        }
                        claim.SupportingDocumentPath = filePath;
                    }

                    _claims.Add(claim);
                    return RedirectToAction("Index", "Dashboard");
                }
                return View(claim);
            }
            catch (Exception ex)
            {
                // Log the exception
                ModelState.AddModelError(string.Empty, "An error occurred while submitting the claim. Please try again.");
                return View(claim);
            }

        }
    }
}
