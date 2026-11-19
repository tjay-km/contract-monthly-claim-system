using CMCS.Controllers;
using CMCS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace CMCS.Tests
{
    [TestFixture]
    public class AccountControllerTests
    {
        private Mock<UserManager<ApplicationUser>> _userManager = null!;
        private Mock<SignInManager<ApplicationUser>> _signInManager = null!;
        private AccountController _controller = null!;

        [SetUp]
        public void SetUp()
        {
            _userManager = TestHelpers.CreateUserManager();
            _signInManager = new Mock<SignInManager<ApplicationUser>>(
                _userManager.Object,
                Mock.Of<IHttpContextAccessor>(),
                Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
                null!, null!, null!, null!);
            _controller = new AccountController(_userManager.Object, _signInManager.Object);
            TestHelpers.SignIn(_controller, "anonymous");
        }

        [TearDown]
        public void TearDown() => _controller.Dispose();

        [Test]
        public async Task Register_AlwaysCreatesALecturer()
        {
            ApplicationUser? created = null;
            _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), "Str0ng!Pass"))
                .Callback<ApplicationUser, string>((user, _) => created = user)
                .ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _controller.Register(new RegisterViewModel
            {
                FirstName = "Ama",
                LastName = "Mensah",
                Email = "ama@test.com",
                UserName = "ama",
                Password = "Str0ng!Pass",
                ConfirmPassword = "Str0ng!Pass",
                Department = "IT"
            });

            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
            Assert.That(created!.Role, Is.EqualTo("Lecturer"));
            _userManager.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Lecturer"), Times.Once);
            _userManager.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(),
                It.IsIn("ProgrammeCoordinator", "AcademicManager")), Times.Never);
        }

        [Test]
        public async Task Login_DeactivatedAccountIsRefusedBeforeCheckingThePassword()
        {
            var user = TestHelpers.User("u1");
            user.IsActive = false;
            _userManager.Setup(m => m.FindByNameAsync("u1")).ReturnsAsync(user);

            var result = await _controller.Login(new LoginViewModel { EmailOrUsername = "u1", Password = "whatever" });

            Assert.That(result, Is.TypeOf<ViewResult>());
            Assert.That(_controller.ModelState.IsValid, Is.False);
            _signInManager.Verify(s => s.PasswordSignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
        }

        [Test]
        public async Task Login_LockedOutUserSeesLockoutPage()
        {
            var user = TestHelpers.User("u2");
            _userManager.Setup(m => m.FindByNameAsync("u2")).ReturnsAsync(user);
            _signInManager.Setup(s => s.PasswordSignInAsync(user, "bad", false, true)).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

            var result = (ViewResult)await _controller.Login(new LoginViewModel { EmailOrUsername = "u2", Password = "bad" });

            Assert.That(result.ViewName, Is.EqualTo("Lockout"));
        }
    }
}
