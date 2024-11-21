using CMCS.Controllers;
using CMCS.Models;
using CMCS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace CMCS.Tests
{
    public class ClaimControllerTests
    {
        private readonly List<CMCS.Models.Claim> _testClaims;
        private readonly Mock<ILogger<ClaimController>> _loggerMock;
        private readonly Mock<IClaimsService> _claimsServiceMock;
        private readonly ClaimController _controller;

        public ClaimControllerTests()
        {
            _testClaims = new List<CMCS.Models.Claim>
            {
                new CMCS.Models.Claim { Id = 1, LecturerName = "John Doe", Status = "Pending", DateSubmitted = DateTime.Now, HoursWorked = 10, HourlyRate = 50m },
                new CMCS.Models.Claim { Id = 2, LecturerName = "Jane Smith", Status = "Approved", DateSubmitted = DateTime.Now.AddDays(-1), HoursWorked = 8, HourlyRate = 45m }
            };

            _loggerMock = new Mock<ILogger<ClaimController>>();
            _claimsServiceMock = new Mock<IClaimsService>();
            _claimsServiceMock.Setup(s => s.GetAllClaimsAsync())
                .ReturnsAsync(_testClaims);
            _claimsServiceMock.Setup(s => s.GetClaimByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => _testClaims.Find(c => c.Id == id) ?? null);

            _controller = new ClaimController(_claimsServiceMock.Object, _loggerMock.Object);
            _controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>()
            );
        }

        [Fact]
        public void Submit_ReturnsViewResult()
        {
            // Arrange
            // Act
            var result = _controller.Submit();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task ListClaims_ReturnsViewResultWithAllClaims()
        {
            // Arrange
            _claimsServiceMock.Setup(s => s.GetAllClaimsAsync())
                .ReturnsAsync(_testClaims);

            // Act
            var result = await _controller.ListClaims() as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<List<CMCS.Models.Claim>>(result.Model);
            Assert.Equal(_testClaims.Count, model.Count);
        }
    }
}
