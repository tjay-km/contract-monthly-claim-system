using CMCS.Controllers;
using CMCS.Models;
using CMCS.Models.Enums;
using CMCS.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using NUnit.Framework;
using ClaimModel = CMCS.Models.Claim;

namespace CMCS.Tests
{
    [TestFixture]
    public class ClaimControllerTests
    {
        private Mock<IClaimsService> _mockClaimsService = null!;
        private Mock<UserManager<ApplicationUser>> _mockUserManager = null!;
        private Mock<IUserStore<ApplicationUser>> _mockUserStore = null!;
        private Mock<ILogger<ClaimController>> _mockLogger = null!;
        private Mock<IDocumentService> _mockDocumentService = null!;
        private Mock<IClaimApprovalService> _mockApprovalService = null!;
        private ClaimController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _mockClaimsService = new Mock<IClaimsService>();
            _mockUserStore = new Mock<IUserStore<ApplicationUser>>();
            _mockLogger = new Mock<ILogger<ClaimController>>();
            _mockDocumentService = new Mock<IDocumentService>();
            _mockApprovalService = new Mock<IClaimApprovalService>();

            // Set up UserManager mock with required options
            var mockOptions = new Mock<IOptions<IdentityOptions>>();
            mockOptions.Setup(x => x.Value).Returns(new IdentityOptions());

            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider();

            var logger = serviceProvider.GetService<ILogger<UserManager<ApplicationUser>>>()!;

            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                _mockUserStore.Object,
                mockOptions.Object,
                new PasswordHasher<ApplicationUser>(),
                Array.Empty<IUserValidator<ApplicationUser>>(),
                Array.Empty<IPasswordValidator<ApplicationUser>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                serviceProvider,
                logger);

            // Set up common approval service mocks
            _mockApprovalService.Setup(x => x.ProcessClaimAsync(It.IsAny<ClaimModel>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            _mockApprovalService.Setup(x => x.ApproveClaimAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            _mockApprovalService.Setup(x => x.RejectClaimAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            // Set up common logger mock
            _mockLogger.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()));
            
            _controller = new ClaimController(
                _mockClaimsService.Object,
                _mockLogger.Object,
                _mockDocumentService.Object,
                _mockApprovalService.Object,
                _mockUserManager.Object);

            // Set up common test user
            var testUser = new ApplicationUser 
            { 
                Id = "1", 
                UserName = "Test1", 
                Name = "Test1", 
                Department = "Test Dept" 
            };

            // Set up common UserManager mock methods
            _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(testUser);
            
            _mockUserManager.Setup(x => x.IsInRoleAsync(It.IsAny<ApplicationUser>(), "Lecturer"))
                .ReturnsAsync(true);
            
            _mockUserManager.Setup(x => x.IsInRoleAsync(It.IsAny<ApplicationUser>(), "ProgrammeCoordinator"))
                .ReturnsAsync(true);
            
            _mockUserManager.Setup(x => x.IsInRoleAsync(It.IsAny<ApplicationUser>(), "AcademicManager"))
                .ReturnsAsync(true);

            // Set up common HttpContext with User
            var user = new ClaimsPrincipal(new ClaimsIdentity(new System.Security.Claims.Claim[]
            {
                new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, testUser.Id),
                new System.Security.Claims.Claim(ClaimTypes.Name, testUser.Name),
                new System.Security.Claims.Claim(ClaimTypes.Role, "Lecturer"),
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();
        }
    }
}
