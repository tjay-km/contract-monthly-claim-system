using Microsoft.AspNetCore.Mvc;
using CMCS.Models;
using System.Collections.Generic;

namespace CMCS.Controllers
{
    public class DashboardController : Controller
    {

        //public IActionResult Index(string role = "Lecturer")
        //{
        //    ViewBag.UserRole = role;
        //    // ... rest of the code
        //}
        public IActionResult Index()
        {
            // Simulating data retrieval
            var claims = new List<Claim>
            {
                new Claim { Id = 1, LecturerName = "John Doe", HoursWorked = 10, HourlyRate = 50, Status = "Pending" },
                new Claim { Id = 2, LecturerName = "Jane Smith", HoursWorked = 15, HourlyRate = 55, Status = "Approved" }
            };

            return View(claims);
        }


    }
}