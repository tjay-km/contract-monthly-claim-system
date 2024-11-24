using CMCS.Models;
using CMCS.Models.Enums;
using NUnit.Framework;
using System;
using System.ComponentModel.DataAnnotations;

namespace CMCS.Tests
{
    [TestFixture]
    public class ClaimModelTests
    {
        [Test]
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
                Status = ClaimStatus.Pending,
                ModuleCode = "CS101",
                SupportingDocumentPath = "/path/to/document.pdf"
            };

            // Assert
            Assert.That(claim.Id, Is.EqualTo(1));
            Assert.That(claim.LecturerId, Is.EqualTo("123"));
            Assert.That(claim.LecturerName, Is.EqualTo("John Doe"));
            Assert.That(claim.LecturerDepartment, Is.EqualTo("Computer Science"));
            Assert.That(claim.HoursWorked, Is.EqualTo(10));
            Assert.That(claim.HourlyRate, Is.EqualTo(50));
            Assert.That(claim.TotalAmount, Is.EqualTo(500));
            Assert.That(claim.AdditionalNotes, Is.EqualTo("Test notes with minimum length"));
            Assert.That(claim.Status, Is.EqualTo(ClaimStatus.Pending));
            Assert.That(claim.ModuleCode, Is.EqualTo("CS101"));
            Assert.That(claim.SupportingDocumentPath, Is.EqualTo("/path/to/document.pdf"));
        }

        [Test]
        [TestCase(10, 50, 500)]    // Regular case
        [TestCase(0.5, 100, 50)]   // Minimum hours
        [TestCase(195, 20000, 3900000)] // Maximum values
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
            Assert.That(claim.TotalAmount, Is.EqualTo(expectedTotal));
        }

        [Test]
        [TestCase(0)]
        [TestCase(20001)]
        public void Claim_HourlyRate_ValidatesRange(decimal rate)
        {
            // Arrange
            var claim = new Claim { HourlyRate = rate };
            var context = new ValidationContext(claim);
            var results = new List<ValidationResult>();

            // Act & Assert
            var isValid = Validator.TryValidateObject(claim, context, results, true);
            Assert.That(isValid, Is.False);
            Assert.That(results.Count, Is.GreaterThan(0));
            Assert.That(results.Exists(r => r.MemberNames.Contains("HourlyRate")), Is.True);
        }

        [Test]
        [TestCase(0.4)]
        [TestCase(195.1)]
        public void Claim_HoursWorked_ValidatesRange(decimal hours)
        {
            // Arrange
            var claim = new Claim { HoursWorked = hours };
            var context = new ValidationContext(claim);
            var results = new List<ValidationResult>();

            // Act & Assert
            var isValid = Validator.TryValidateObject(claim, context, results, true);
            Assert.That(isValid, Is.False);
            Assert.That(results.Count, Is.GreaterThan(0));
            Assert.That(results.Exists(r => r.MemberNames.Contains("HoursWorked")), Is.True);
        }
    }
}
