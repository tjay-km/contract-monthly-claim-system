using CMCS.Controllers;
using CMCS.Models;
using CMCS.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using Xunit;

namespace CMCS.Tests
{
    public class ClaimsControllerTests
    {
        private readonly List<Claim> _testClaims;
        private readonly IClaimsService _claimsService; // Change to IClaimsService interface
        private readonly ClaimsController _controller;

        public ClaimsControllerTests()
        {
            _testClaims = new List<Claim>
            {
                new Claim { Id = 1, LecturerName = "John Doe", Status = "Pending", DateSubmitted = DateTime.Now, HoursWorked = 10, HourlyRate = 50m },
                new Claim { Id = 2, LecturerName = "Jane Smith", Status = "Approved", DateSubmitted = DateTime.Now.AddDays(-1), HoursWorked = 8, HourlyRate = 60m }
            };

            _claimsService = new ClaimsService(_testClaims); // Use concrete ClaimsService implementing IClaimsService
            _controller = new ClaimsController(_claimsService); // Inject the service into the controller
        }

        [Fact]
        public void UpdateClaimStatus_ReturnsSuccess_WhenUpdateIsSuccessful()
        {
            // Arrange
            int claimId = 1;
            string status = "Approved";

            // Act
            var result = _controller.UpdateClaimStatus(claimId, status) as JsonResult;

            // Assert
            Assert.NotNull(result);
            dynamic jsonResult = result.Value;
            Assert.True((bool)jsonResult.success);
            Assert.Equal("Claim status updated successfully.", (string)jsonResult.message);

            var updatedClaim = _claimsService.GetClaimById(claimId);
            Assert.Equal(status, updatedClaim.Status);
        }

        [Fact]
        public void UpdateClaimStatus_ReturnsError_WhenClaimNotFound()
        {
            // Arrange
            int claimId = 99; // Non-existent ID
            string status = "Approved";

            // Act
            var result = _controller.UpdateClaimStatus(claimId, status) as JsonResult;

            // Assert
            Assert.NotNull(result);
            dynamic jsonResult = result.Value;
            Assert.False((bool)jsonResult.success);
            Assert.Equal("Error updating claim status.", (string)jsonResult.message);
        }

        [Theory]
        [InlineData(0, "Approved")]
        [InlineData(-1, "Rejected")]
        [InlineData(1, "")]
        [InlineData(1, null)]
        public void UpdateClaimStatus_ReturnsBadRequest_WhenInputIsInvalid(int claimId, string status)
        {
            // Act
            var result = _controller.UpdateClaimStatus(claimId, status);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result); // Expecting BadRequest
        }

    }
}
