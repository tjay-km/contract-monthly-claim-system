using CMCS.Controllers;
using CMCS.Models;
using CMCS.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using CMCS.Models.Enums;
using CMCS.Data;

namespace CMCS.Tests
{
    public class ClaimControllerTests
    {
        private readonly List<CMCS.Models.Claim> _testClaims;
        private readonly Mock<IClaimsService> _mockClaimsService;
        private readonly Mock<ILogger<ClaimController>> _mockLogger;
        private readonly Mock<IDocumentService> _mockDocumentService;
        private readonly Mock<IClaimApprovalService> _mockApprovalService;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<ApplicationDbContext> _mockContext;
        private readonly ClaimController _controller;

        public ClaimControllerTests()
        {
            _testClaims = new List<CMCS.Models.Claim>
            {
                new CMCS.Models.Claim { 
                    Id = 1, 
                    LecturerName = "John Doe", 
                    Status = ClaimStatus.Pending, 
                    SubmissionDate = DateTime.Now, 
                    HoursWorked = 10, 
                    HourlyRate = 50m,
                    LecturerDepartment = "Computer Science",
                    ModuleCode = "CS101",
                    AdditionalNotes = "Test notes with minimum length"
                },
                new CMCS.Models.Claim { 
                    Id = 2, 
                    LecturerName = "Jane Smith", 
                    Status = ClaimStatus.Approved, 
                    SubmissionDate = DateTime.Now.AddDays(-1), 
                    HoursWorked = 8, 
                    HourlyRate = 45m,
                    LecturerDepartment = "Mathematics",
                    ModuleCode = "MTH201",
                    AdditionalNotes = "Test notes with minimum length"
                }
            };

            _mockClaimsService = new Mock<IClaimsService>();
            _mockLogger = new Mock<ILogger<ClaimController>>();
            _mockDocumentService = new Mock<IDocumentService>();
            _mockApprovalService = new Mock<IClaimApprovalService>();
            _mockContext = new Mock<ApplicationDbContext>();

            // Setup mock user manager
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                userStore.Object, null, null, null, null, null, null, null, null);

            _mockClaimsService.Setup(s => s.GetAllClaimsAsync())
                .ReturnsAsync(_testClaims);
            _mockClaimsService.Setup(s => s.GetClaimByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => _testClaims.Find(c => c.Id == id));

            _controller = new ClaimController(
                _mockClaimsService.Object, 
                _mockLogger.Object, 
                _mockDocumentService.Object,
                _mockApprovalService.Object,
                _mockUserManager.Object,
                _mockContext.Object);
        }

        [Fact]
        public async Task ListClaims_ReturnsViewWithClaims()
        {
            // Act
            var result = await _controller.ListClaims() as ViewResult;
            var model = result?.Model as IEnumerable<CMCS.Models.Claim>;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(model);
            Assert.Equal(_testClaims, model);
        }

        [Fact]
        public async Task Submit_Get_ReturnsViewWithNewClaim()
        {
            // Arrange
            var userId = "testUserId";
            var userName = "Test User";
            var user = new ApplicationUser { Id = userId, Name = userName };

            _mockUserManager.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.Submit() as ViewResult;
            var claim = result?.Model as CMCS.Models.Claim;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(claim);
            Assert.Equal(userId, claim.LecturerId);
            Assert.Equal(userName, claim.LecturerName);
            Assert.Equal(ClaimStatus.Pending, claim.Status);
        }

        [Fact]
        public async Task Submit_Post_WithInvalidModel_ReturnsViewWithSameModel()
        {
            // Arrange
            var claim = new CMCS.Models.Claim();
            _controller.ModelState.AddModelError("", "Test error");

            // Act
            var result = await _controller.Submit(claim, null) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(claim, result.Model);
        }
    }
}
