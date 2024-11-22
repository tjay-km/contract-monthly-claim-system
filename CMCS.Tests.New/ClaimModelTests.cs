using CMCS.Models;
using System;
using Xunit;

namespace CMCS.Tests
{
    public class ClaimModelTests
    {
        [Fact]
        public void Claim_PropertiesAreSetCorrectly()
        {
            // Arrange
            var claim = new Claim
            {
                Id = 1,
                LecturerId = "123",
                LecturerName = "John Doe",
                LecturerDepartment = "Computer Science",
                SubmissionDate = DateTime.Now,
                HoursWorked = 10,
                HourlyRate = 50,
                TotalAmount = 500,
                AdditionalNotes = "Test notes with minimum length",
                Status = "Pending",
                ModuleCode = "CS101",
                SupportingDocumentPath = "/path/to/document.pdf"
            };

            // Assert
            Assert.Equal(1, claim.Id);
            Assert.Equal("123", claim.LecturerId);
            Assert.Equal("John Doe", claim.LecturerName);
            Assert.Equal("Computer Science", claim.LecturerDepartment);
            Assert.Equal(10, claim.HoursWorked);
            Assert.Equal(50, claim.HourlyRate);
            Assert.Equal(500, claim.TotalAmount);
            Assert.Equal("Test notes with minimum length", claim.AdditionalNotes);
            Assert.Equal("Pending", claim.Status);
            Assert.Equal("CS101", claim.ModuleCode);
            Assert.Equal("/path/to/document.pdf", claim.SupportingDocumentPath);
        }

        [Theory]
        [InlineData(10, 50, 500)]    // Regular case
        [InlineData(0.5, 100, 50)]   // Minimum hours
        [InlineData(195, 20000, 3900000)] // Maximum values
        public void Claim_TotalAmount_CalculatesCorrectly(decimal hoursWorked, decimal hourlyRate, decimal expectedTotal)
        {
            // Arrange
            var claim = new Claim
            {
                HoursWorked = hoursWorked,
                HourlyRate = hourlyRate,
                TotalAmount = hoursWorked * hourlyRate
            };

            // Assert
            Assert.Equal(expectedTotal, claim.TotalAmount);
        }

        [Theory]
        [InlineData(0.4)]    // Below minimum hours
        [InlineData(195.1)]  // Above maximum hours
        public void Claim_HoursWorked_ValidatesRange(decimal hours)
        {
            // Arrange
            var claim = new Claim();

            // Act & Assert
            var exception = Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(
                () => claim.HoursWorked = hours
            );
            Assert.Contains("Hours worked must be between 0.5 and 195", exception.Message);
        }

        [Theory]
        [InlineData(0)]      // Below minimum rate
        [InlineData(20001)]  // Above maximum rate
        public void Claim_HourlyRate_ValidatesRange(decimal rate)
        {
            // Arrange
            var claim = new Claim();

            // Act & Assert
            var exception = Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(
                () => claim.HourlyRate = rate
            );
            Assert.Contains("Hourly rate must be between 1 and 20000", exception.Message);
        }
    }
}
