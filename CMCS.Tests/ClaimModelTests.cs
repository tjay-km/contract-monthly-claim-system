using CMCS.Models;
using System;
using Xunit;

public class ClaimModelTests
{
    [Fact]
    public void Claim_PropertiesAreSetCorrectly()
    {
        // Arrange
        var claim = new Claim
        {
            Id = 1,
            LecturerName = "John Doe",
            DateSubmitted = DateTime.Now,
            HoursWorked = 10,
            HourlyRate = 50,
            AdditionalNotes = "Test notes",
            Status = "Pending",
            SupportingDocumentPath = "/path/to/document.pdf"
        };

        // Assert
        Assert.Equal(1, claim.Id);
        Assert.Equal("John Doe", claim.LecturerName);
        Assert.Equal(10, claim.HoursWorked);
        Assert.Equal(50, claim.HourlyRate);
        Assert.Equal("Test notes", claim.AdditionalNotes);
        Assert.Equal("Pending", claim.Status);
        Assert.Equal("/path/to/document.pdf", claim.SupportingDocumentPath);
    }

    [Theory]
    [InlineData(0, 50, 0)]
    [InlineData(10, 0, 0)]
    [InlineData(10, 50, 500)]
    public void Claim_CalculateTotalAmount_ReturnsCorrectValue(decimal hoursWorked, decimal hourlyRate, decimal expectedTotal)
    {
        // Arrange
        var claim = new Claim
        {
            HoursWorked = hoursWorked,
            HourlyRate = hourlyRate
        };

        // Act
        decimal totalAmount = claim.CalculateTotalAmount();

        // Assert
        Assert.Equal(expectedTotal, totalAmount);
    }
}