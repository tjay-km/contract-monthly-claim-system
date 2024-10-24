using CMCS.Models;
using CMCS.Services;
using System;
using System.Collections.Generic;
using Xunit;

namespace CMCS.Tests
{
    public class ClaimsServiceTests
    {
        private readonly List<Claim> _testClaims;
        private readonly ClaimsService _service;

        public ClaimsServiceTests()
        {
            _testClaims = new List<Claim>
            {
                new Claim { Id = 1, LecturerName = "John Doe", Status = "Pending", DateSubmitted = DateTime.Now, HoursWorked = 10, HourlyRate = 50 },
                new Claim { Id = 2, LecturerName = "Jane Smith", Status = "Approved", DateSubmitted = DateTime.Now.AddDays(-1), HoursWorked = 8, HourlyRate = 60 },
                new Claim { Id = 3, LecturerName = "Bob Johnson", Status = "Rejected", DateSubmitted = DateTime.Now.AddDays(-2), HoursWorked = 12, HourlyRate = 45 }
            };
            _service = new ClaimsService(_testClaims);
        }

        [Fact]
        public void GetClaims_ReturnsAllClaims()
        {
            // Act
            var result = _service.GetClaims();

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal(_testClaims, result);
        }

        [Fact]
        public void GetClaimById_ReturnsCorrectClaim()
        {
            // Act
            var result = _service.GetClaimById(2);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Jane Smith", result.LecturerName);
        }

        [Fact]
        public void GetClaimById_ReturnsNullForNonexistentId()
        {
            // Act
            var result = _service.GetClaimById(99);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void UpdateClaimStatus_UpdatesStatusCorrectly()
        {
            // Arrange
            int claimId = 1;
            string newStatus = "Approved";

            // Act
            _service.UpdateClaimStatus(claimId, newStatus);

            // Assert
            var updatedClaim = _service.GetClaimById(claimId);
            Assert.Equal(newStatus, updatedClaim.Status);
        }

        [Fact]
        public void UpdateClaimStatus_DoesNothingForNonexistentId()
        {
            // Arrange
            int nonexistentId = 99;
            string newStatus = "Approved";

            // Act
            _service.UpdateClaimStatus(nonexistentId, newStatus);

            // Assert
            var claim = _service.GetClaimById(nonexistentId);
            Assert.Null(claim);
        }

        [Fact]
        public void GetPendingClaims_ReturnsOnlyPendingClaims()
        {
            // Act
            var result = _service.GetPendingClaims();

            // Assert
            Assert.Single(result);
            Assert.All(result, claim => Assert.Equal("Pending", claim.Status));
        }

        [Fact]
        public void GetManagedClaims_ReturnsOnlyApprovedAndRejectedClaims()
        {
            // Act
            var result = _service.GetManagedClaims();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, claim => Assert.Contains(claim.Status, new[] { "Approved", "Rejected" }));
        }
    }
}