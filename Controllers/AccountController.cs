using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CMCS.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace CMCS.Controllers
{
    public class AccountController : Controller
    {
        public static List<User> Users = new List<User>();

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(User newUser, string confirmPassword)
        {
            ModelState.Clear();

            if (string.IsNullOrWhiteSpace(newUser.Name))
            {
                ModelState.AddModelError(nameof(newUser.Name), "Name is required.");
            }

            if (!IsValidUsernameOrEmail(newUser.EmailOrUsername))
            {
                ModelState.AddModelError(nameof(newUser.EmailOrUsername), "Please enter a valid username or email address.");
            }

            if (newUser.Password.Length < 8)
            {
                ModelState.AddModelError(nameof(newUser.Password), "Password must be at least 8 characters long.");
            }

            if (newUser.Password != confirmPassword)
            {
                ModelState.AddModelError(nameof(confirmPassword), "Passwords do not match.");
            }

            var existingUser = Users.FirstOrDefault(u => u.EmailOrUsername == newUser.EmailOrUsername);
            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "Username or Email already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(newUser);
            }

            var hasher = new PasswordHasher<User>();
            newUser.Password = hasher.HashPassword(newUser, newUser.Password);

            Users.Add(newUser);

            TempData["SuccessMessage"] = "You have successfully registered! Welcome to Contract Monthly Claim Systems. Redirecting to login page in 5 seconds...";
            return View(newUser);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string usernameOrEmail, string password)
        {
            var user = Users.FirstOrDefault(u => u.EmailOrUsername == usernameOrEmail);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid username/email or password.");
                return View();
            }

            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(user, user.Password, password);

            if (result == PasswordVerificationResult.Success)
            {
                HttpContext.Session.SetString("UserRole", user.Role);
                switch (user.Role)
                {
                    case "Lecturer":
                        return RedirectToAction("LecturerDashboard", "Dashboard");
                    case "ProgrammeCoordinator":
                        return RedirectToAction("CoordinatorDashboard", "Dashboard");
                    case "AcademicManager":
                        return RedirectToAction("ManagerDashboard", "Dashboard");
                    default:
                        return RedirectToAction("Index", "Home");
                }
            }

            ModelState.AddModelError(string.Empty, "Invalid username/email or password.");
            return View();
        }

        private bool IsValidUsernameOrEmail(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (emailRegex.IsMatch(input))
            {
                return true;
            }

            var usernameRegex = new Regex(@"^[a-zA-Z0-9]+$");
            return usernameRegex.IsMatch(input);
        }
    }
}