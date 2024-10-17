using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using CMCS.Models;

public class ClaimController : Controller
{
    private static List<Claim> _claims = new List<Claim>();

    public IActionResult Index()
    {
        return View(_claims);
    }

    [HttpGet]
    public IActionResult Submit()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Submit(Claim claim, IFormFile supportingDocument)
    {
        if (ModelState.IsValid)
        {
            claim.Id = _claims.Count + 1;
            claim.Status = "Pending";

            if (supportingDocument != null)
            {
                // Logic to save the file and set the path
                var filePath = Path.Combine("uploads", supportingDocument.FileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    supportingDocument.CopyTo(stream);
                }
                claim.SupportingDocumentPath = filePath;
            }

            _claims.Add(claim);
            return RedirectToAction("Index");
        }
        return View(claim);
    }

    [HttpPost]
    public IActionResult Approve(int id)
    {
        var claim = _claims.FirstOrDefault(c => c.Id == id);
        if (claim != null)
        {
            claim.Status = "Approved";
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult Reject(int id)
    {
        var claim = _claims.FirstOrDefault(c => c.Id == id);
        if (claim != null)
        {
            claim.Status = "Rejected";
        }
        return RedirectToAction("Index");
    }
}
