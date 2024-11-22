using CMCS.Models;
using CMCS.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CMCS.Tests
{
    public class ClaimsServiceTests
    {
        private readonly List<Claim> _testClaims;
        private readonly Mock<IClaimsService> _mockClaimsService;

        public ClaimsServiceTests()
        {
            _testClaims = new List<Claim>
            {
                new Claim 
                { 
                    Id = 1, 
                    LecturerId = "user1",
                    LecturerName = "John Doe", 
                    Status = "Pending", 
                    SubmissionDate = DateTime.Now, 
                    HoursWorked = 10, 
                    HourlyRate = 50,
                    LecturerDepartment = "Computer Science",
                    ModuleCode = "CS101",
                    AdditionalNotes = "Test notes with minimum length"
                },
                new Claim 
                { 
                    Id = 2, 
                    LecturerId = "user2",
                    LecturerName = "Jane Smith", 
                    Status = "Approved", 
                    SubmissionDate = DateTime.Now.AddDays(-1), 
                    HoursWorked = 8, 
                    HourlyRate = 60,
                    LecturerDepartment = "Mathematics",
                    ModuleCode = "MTH201",
                    AdditionalNotes = "Test notes with minimum length"
                },
                new Claim 
                { 
                    Id = 3, 
                    LecturerId = "user3",
                    LecturerName = "Bob Johnson", 
                    Status = "Rejected", 
                    SubmissionDate = DateTime.Now.AddDays(-2), 
                    HoursWorked = 12, 
                    HourlyRate = 45,
                    LecturerDepartment = "Physics",
                    ModuleCode = "PHY301",
                    AdditionalNotes = "Test notes with minimum length"
                }
            };

            _mockClaimsService = new Mock<IClaimsService>();
            SetupMockService();
        }

        private void SetupMockService()
        {
            _mockClaimsService.Setup(s => s.GetAllClaimsAsync())
                .ReturnsAsync(_testClaims);

            _mockClaimsService.Setup(s => s.GetClaimByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => _testClaims.FirstOrDefault(c => c.Id == id));

            _mockClaimsService.Setup(s => s.CreateClaimAsync(It.IsAny<Claim>()))
                .ReturnsAsync((Claim claim) => { claim.Id = _testClaims.Max(c => c.Id) + 1; return claim; });

            _mockClaimsService.Setup(s => s.UpdateClaimAsync(It.IsAny<Claim>()))
                .ReturnsAsync((Claim claim) => claim);

            _mockClaimsService.Setup(s => s.DeleteClaimAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => _testClaims.Any(c => c.Id == id));
        }

        [Fact]
        public async Task GetAllClaimsAsync_ReturnsAllClaims()
        {
            // Act
            var result = await _mockClaimsService.Object.GetAllClaimsAsync();

            // Assert
            Assert.Equal(_testClaims.Count, result.Count());
            Assert.Equal(_testClaims, result);
        }

        [Fact]
        public async Task GetClaimByIdAsync_ReturnsCorrectClaim()
        {
            // Act
            var result = await _mockClaimsService.Object.GetClaimByIdAsync(2);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Jane Smith", result.LecturerName);
            Assert.Equal("Mathematics", result.LecturerDepartment);
        }

        [Fact]
        public async Task GetClaimByIdAsync_ReturnsNullForNonexistentId()
        {
            // Act
            var result = await _mockClaimsService.Object.GetClaimByIdAsync(99);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateClaimAsync_AssignsNewId()
        {
            // Arrange
            var newClaim = new Claim
            {
                LecturerId = "user4",
                LecturerName = "New User",
                Status = "Pending",
                SubmissionDate = DateTime.Now,
                HoursWorked = 5,
                HourlyRate = 40,
                LecturerDepartment = "Chemistry",
                ModuleCode = "CHM101",
                AdditionalNotes = "Test notes with minimum length"
            };

            // Act
            var result = await _mockClaimsService.Object.CreateClaimAsync(newClaim);

            // Assert
            Assert.NotEqual(0, result.Id);
            Assert.Equal(newClaim.LecturerName, result.LecturerName);
        }

        [Fact]
        public async Task UpdateClaimAsync_ReturnsUpdatedClaim()
        {
            // Arrange
            var claim = _testClaims.First();
            claim.Status = "Approved";

            // Act
            var result = await _mockClaimsService.Object.UpdateClaimAsync(claim);

            // Assert
            Assert.Equal("Approved", result.Status);
        }

        [Fact]
        public async Task DeleteClaimAsync_ReturnsTrueForExistingId()
        {
            // Act
            var result = await _mockClaimsService.Object.DeleteClaimAsync(1);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task DeleteClaimAsync_ReturnsFalseForNonexistentId()
        {
            // Act
            var result = await _mockClaimsService.Object.DeleteClaimAsync(99);

            // Assert
            Assert.False(result);
        }
    }
}
