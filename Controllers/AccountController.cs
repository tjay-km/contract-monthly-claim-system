using System.Threading.Tasks;
using CMCS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CMCS.Controllers
{
    public class AccountController : Controller
    {
        /// <summary>
        /// Self-registration always creates a lecturer. Coordinator and manager accounts are
        /// created by an administrator (seeded in development).
        /// </summary>
        public const string SelfRegistrationRole = "Lecturer";

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Name = $"{model.FirstName} {model.LastName}",
                Department = model.Department,
                Role = SelfRegistrationRole,
                UserType = UserType.Lecturer
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
                result = await _userManager.AddToRoleAsync(user, SelfRegistrationRole);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Registration successful! Please log in to continue.";
                return RedirectToAction(nameof(Login));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByNameAsync(model.EmailOrUsername)
                       ?? await _userManager.FindByEmailAsync(model.EmailOrUsername);
            if (user is { IsActive: false })
            {
                ModelState.AddModelError(string.Empty, "This account has been deactivated. Please contact HR.");
                return View(model);
            }

            var result = user == null
                ? Microsoft.AspNetCore.Identity.SignInResult.Failed
                : await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
                return RedirectToAction("Index", "Home");
            if (result.IsLockedOut)
                return View("Lockout");

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
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
        public IActionResult AccessDenied() => View();
    }
}
