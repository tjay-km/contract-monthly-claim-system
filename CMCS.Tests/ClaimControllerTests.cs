using CMCS.Tests;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

public class ClaimsControllerTests
{
    private readonly Mock<IClaimService> _mockClaimService;
    private readonly ClaimsController _controller;

    public ClaimsControllerTests()
    {
        _mockClaimService = new Mock<IClaimService>();
        _controller = new ClaimsController(_mockClaimService.Object);
    }

    [Fact]
    public void UpdateClaimStatus_ReturnsSuccess_WhenUpdateIsSuccessful()
    {
        // Arrange
        int claimId = 1;
        string status = "Approved";
        _mockClaimService.Setup(s => s.UpdateStatus(claimId, status)).Returns(true);

        // Act
        var result = _controller.UpdateClaimStatus(claimId, status) as JsonResult;

        // Assert
        Assert.NotNull(result);
        Assert.True((bool)result.Value.GetType().GetProperty("success").GetValue(result.Value));
        Assert.Equal("Claim status updated successfully.", result.Value.GetType().GetProperty("message").GetValue(result.Value));
    }

    [Fact]
    public void UpdateClaimStatus_ReturnsError_WhenUpdateFails()
    {
        // Arrange
        int claimId = 1;
        string status = "Rejected";
        _mockClaimService.Setup(s => s.UpdateStatus(claimId, status)).Returns(false);

        // Act
        var result = _controller.UpdateClaimStatus(claimId, status) as JsonResult;

        // Assert
        Assert.NotNull(result);
        Assert.False((bool)result.Value.GetType().GetProperty("success").GetValue(result.Value));
        Assert.Equal("Error updating claim status.", result.Value.GetType().GetProperty("message").GetValue(result.Value));
    }
}
