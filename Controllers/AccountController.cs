using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using CMCS.Models;
using CMCS.Services;
using System.Collections.Generic;
using CMCS.Data;
using Microsoft.EntityFrameworkCore;

namespace CMCS.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IIdentityService _identityService;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _dbContext;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IIdentityService identityService,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _identityService = identityService;
            _roleManager = roleManager;
            _dbContext = dbContext;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Normalize and validate input
                    var normalizedUserName = model.UserName?.Trim().ToUpper();
                    var normalizedEmail = model.Email?.Trim().ToUpper();
                    var normalizedName = model.Name?.Trim();
                    var normalizedRole = model.Role?.Trim() ?? "Lecturer";

                    if (string.IsNullOrEmpty(normalizedUserName) || 
                        string.IsNullOrEmpty(normalizedEmail) || 
                        string.IsNullOrEmpty(normalizedName))
                    {
                        ModelState.AddModelError(string.Empty, "All fields are required.");
                        return View(model);
                    }

                    // Check if user exists using direct database context
                    var existingUser = await _dbContext.Users
                        .AsNoTracking()
                        .Where(u => u.NormalizedUserName == normalizedUserName || u.NormalizedEmail == normalizedEmail)
                        .FirstOrDefaultAsync();

                    if (existingUser != null)
                    {
                        ModelState.AddModelError(string.Empty, "A user with this email or username already exists.");
                        return View(model);
                    }

                    // Create new user with non-null values
                    var user = new ApplicationUser
                    {
                        UserName = model.UserName.Trim(),
                        NormalizedUserName = normalizedUserName,
                        Email = model.Email.Trim(),
                        NormalizedEmail = normalizedEmail,
                        Name = normalizedName,
                        Role = normalizedRole,
                        UserType = GetUserType(normalizedRole),
                        IsActive = true,
                        EmailConfirmed = true,
                        SecurityStamp = Guid.NewGuid().ToString("D"),
                        ConcurrencyStamp = Guid.NewGuid().ToString("D"),
                        LockoutEnabled = true,
                        PhoneNumberConfirmed = false
                    };

                    // Try to create the user
                    var result = await _userManager.CreateAsync(user, model.Password);
                    if (result.Succeeded)
                    {
                        try
                        {
                            // Create role if it doesn't exist
                            if (!await _roleManager.RoleExistsAsync(normalizedRole))
                            {
                                var identityRole = new IdentityRole(normalizedRole)
                                {
                                    NormalizedName = normalizedRole.ToUpper()
                                };
                                var roleResult = await _roleManager.CreateAsync(identityRole);
                                if (!roleResult.Succeeded)
                                {
                                    throw new Exception($"Failed to create role: {normalizedRole}");
                                }
                            }

                            // Add user to role
                            var addToRoleResult = await _userManager.AddToRoleAsync(user, normalizedRole);
                            if (addToRoleResult.Succeeded)
                            {
                                TempData["SuccessMessage"] = "Registration successful! Please log in to continue.";
                                return RedirectToAction("Login", "Account");
                            }
                            else
                            {
                                // If role assignment fails, delete the user and show error
                                await _userManager.DeleteAsync(user);
                                ModelState.AddModelError(string.Empty, "Failed to assign role to user. Please try again.");
                            }
                        }
                        catch (Exception ex)
                        {
                            // If any role operations fail, delete the user
                            await _userManager.DeleteAsync(user);
                            ModelState.AddModelError(string.Empty, "Failed to complete registration. Please try again.");
                            Console.WriteLine($"Role operation error: {ex.Message}");
                        }
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Registration error: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    ModelState.AddModelError(string.Empty, 
                        "An error occurred during registration. Please try again. " +
                        "If the problem persists, contact support.");
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Normalize the input to avoid case sensitivity issues
                    var normalizedInput = model.EmailOrUsername?.Trim();
                    if (string.IsNullOrEmpty(normalizedInput))
                    {
                        ModelState.AddModelError(string.Empty, "Email or username is required.");
                        return View(model);
                    }

                    // Try to find the user by email first
                    var user = await _userManager.FindByEmailAsync(normalizedInput);

                    // If not found by email, try by username
                    if (user == null)
                    {
                        user = await _userManager.FindByNameAsync(normalizedInput);
                    }

                    // If still no user found, return error
                    if (user == null)
                    {
                        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                        return View(model);
                    }

                    // Ensure the user has required fields
                    if (string.IsNullOrEmpty(user.UserName))
                    {
                        ModelState.AddModelError(string.Empty, "User account is invalid. Please contact support.");
                        return View(model);
                    }

                    var result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, lockoutOnFailure: true);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("Index", "Home");
                    }
                    if (result.IsLockedOut)
                    {
                        return View("Lockout");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                        return View(model);
                    }
                }
                catch (Exception ex)
                {
                    // Log the error
                    ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
                    return View(model);
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private UserType GetUserType(string role)
        {
            return role?.ToUpper() switch
            {
                "LECTURER" => UserType.Lecturer,
                "PROGRAMMECOORDINATOR" => UserType.HOD,
                "ACADEMICMANAGER" => UserType.HR,
                _ => UserType.Lecturer
            };
        }
    }
}
