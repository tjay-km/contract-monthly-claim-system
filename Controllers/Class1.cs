//using System;
//using Microsoft.AspNetCore.Mvc;
//using CMCS.Models;
//using System.Collections.Generic;

//public class ClaimController : Controller
//{
//    // Temporary list to simulate a database
//    private static List<Claim> _claims = new List<Claim>();

//    // Submit a claim - Lecturer
//    [HttpGet]
//    public IActionResult SubmitClaim()
//    {
//        return View();
//    }

//    [HttpPost]
//    public IActionResult SubmitClaim(Claim claim)
//    {
//        // Set initial claim status and add claim to the list
//        claim.Status = "Pending";
//        claim.SubmissionDate = DateTime.Now;
//        _claims.Add(claim);

//        return RedirectToAction("ClaimStatus", new { claimId = claim.ClaimId });
//    }

//    // Track Claim Status - Lecturer
//    public IActionResult ClaimStatus(int claimId)
//    {
//        var claim = _claims.Find(c => c.ClaimId == claimId);
//        return View(claim);
//    }

//    // Review Claims - Programme Coordinator/Admin
//    public IActionResult ReviewClaims()
//    {
//        return View(_claims);
//    }

//    // Approve/Reject Claim - Programme Coordinator/Admin
//    public IActionResult ApproveRejectClaim(int claimId, bool approve)
//    {
//        var claim = _claims.Find(c => c.ClaimId == claimId);
//        if (approve)
//        {
//            claim.Status = "Approved";
//        }
//        else
//        {
//            claim.Status = "Rejected";
//        }
//        return RedirectToAction("ReviewClaims");
//    }
//}
