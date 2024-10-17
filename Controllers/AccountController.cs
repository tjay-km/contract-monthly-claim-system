using Microsoft.AspNetCore.Mvc;
using CMCS.Models;
using System.Linq;
using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CMCS.Controllers
{
    public class AccountController : Controller
    {
        private readonly List<User> _users;

        public AccountController(List<User> users)
        {
            _users = users;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(User newUser, string confirmPassword)
        {
            ModelState.Clear();

            // Check for required fields
            if (string.IsNullOrWhiteSpace(newUser.Name) || string.IsNullOrWhiteSpace(newUser.EmailOrUsername) ||
                string.IsNullOrWhiteSpace(newUser.Password) || string.IsNullOrWhiteSpace(newUser.Role))
            {
                ModelState.AddModelError(string.Empty, "All fields are required.");
                return View(newUser);
            }

            // Validate Email or Username
            if (!IsValidUsernameOrEmail(newUser.EmailOrUsername))
            {
                ModelState.AddModelError(nameof(newUser.EmailOrUsername), "Please enter a valid username or email address.");
            }

            // Check if the password meets length requirements
            if (newUser.Password.Length < 8)
            {
                ModelState.AddModelError(nameof(newUser.Password), "Password must be at least 8 characters long.");
            }

            // Check if passwords match
            if (newUser.Password != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Passwords do not match.");
                return View(newUser);
            }

            // Check if the username or email already exists
            var existingUser = _users.FirstOrDefault(u => u.EmailOrUsername == newUser.EmailOrUsername);
            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "Username or Email already exists.");
                return View(newUser);
            }

            // Hash the password
            var hasher = new PasswordHasher<User>();
            newUser.Password = hasher.HashPassword(newUser, newUser.Password);

            // Add new user to the in-memory list
            newUser.Id = _users.Count + 1;
            _users.Add(newUser);

            TempData["SuccessMessage"] = "You have successfully registered! Welcome to Contract Monthly Claim System. Redirecting to login page...";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string usernameOrEmail, string password)
        {
            // Find user by username or email
            var user = _users.FirstOrDefault(u => u.EmailOrUsername == usernameOrEmail && u.Password == password);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View();
            }

            // Verify the password
            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(user, user.Password, password);

            if (result == PasswordVerificationResult.Success)
            {
                // Create claims and sign in the user
                var claims = new List<System.Security.Claims.Claim> // Use the full namespace here
                {
                    new System.Security.Claims.Claim(ClaimTypes.Name, user.Name),
                    new System.Security.Claims.Claim(ClaimTypes.Email, user.EmailOrUsername),
                    new System.Security.Claims.Claim(ClaimTypes.Role, user.Role)
                };


                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                //// Redirect based on the role of the user
                //switch (user.Role)
                //{
                //    case "Lecturer":
                //        return RedirectToAction("LecturerDashboard", "Dashboard");
                //    case "ProgrammeCoordinator":
                //        return RedirectToAction("CoordinatorDashboard", "Dashboard");
                //    case "AcademicManager":
                //        return RedirectToAction("ManagerDashboard", "Dashboard");
                //    default:
                //        return RedirectToAction("Index", "Home");
                //}
                return RedirectToAction("Index", "Dashboard");
            }

            ModelState.AddModelError(string.Empty, "Invalid username/email or password.");
            return View();
        }

        //[HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        private bool IsValidUsernameOrEmail(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            // Validate Email
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (emailRegex.IsMatch(input))
            {
                return true;
            }

            // Validate Username (alphanumeric only)
            var usernameRegex = new Regex(@"^[a-zA-Z0-9]+$");
            return usernameRegex.IsMatch(input);
        }
    }
}
