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

            if (string.IsNullOrWhiteSpace(newUser.Name) || string.IsNullOrWhiteSpace(newUser.EmailOrUsername) ||
                string.IsNullOrWhiteSpace(newUser.Password) || string.IsNullOrWhiteSpace(newUser.Role))
            {
                ModelState.AddModelError(string.Empty, "All fields are required.");
                return View(newUser);
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
                ModelState.AddModelError(string.Empty, "Passwords do not match.");
                return View(newUser);
            }

            var existingUser = _users.FirstOrDefault(u => u.EmailOrUsername == newUser.EmailOrUsername);
            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "Username or Email already exists.");
                return View(newUser);
            }

            var hasher = new PasswordHasher<User>();
            newUser.Password = hasher.HashPassword(newUser, newUser.Password);

            newUser.Id = _users.Count + 1; // Assign a unique ID
            _users.Add(newUser); // Store the new user

            TempData["SuccessMessage"] = "You have successfully registered! Welcome to Contract Monthly Claim System. Redirecting to login page...";
            TempData.Keep("SuccessMessage"); // Keep the success message for one more request

            return View(newUser); // Show success message before redirect
        }



        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string usernameOrEmail, string password)
        {
            var user = _users.FirstOrDefault(u => u.EmailOrUsername == usernameOrEmail);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View();
            }

            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(user, user.Password, password);

            if (result == PasswordVerificationResult.Success)
            {
                var claims = new List<System.Security.Claims.Claim>
                {
                    new System.Security.Claims.Claim(ClaimTypes.Name, user.Name),
                    new System.Security.Claims.Claim(ClaimTypes.Email, user.EmailOrUsername),
                    new System.Security.Claims.Claim(ClaimTypes.Role, user.Role)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("Index", "Dashboard");
            }

            ModelState.AddModelError(string.Empty, "Invalid username/email or password.");
            return View();
        }

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
