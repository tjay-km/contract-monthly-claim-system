using CMCS.Controllers;
using CMCS.Models;
using CMCS.Models.Enums;
using CMCS.Models.ViewModels;
using CMCS.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using ClaimModel = CMCS.Models.Claim;

namespace CMCS.Tests
{
    [TestFixture]
    public class ClaimControllerTests
    {
        private Mock<IClaimsService> _claims = null!;
        private Mock<IDocumentService> _documents = null!;
        private Mock<IClaimApprovalService> _approval = null!;
        private Mock<IClaimValidationService> _validation = null!;
        private Mock<UserManager<ApplicationUser>> _userManager = null!;
        private ClaimController _controller = null!;
        private readonly ApplicationUser _lecturer = TestHelpers.User("lecturer-1", "Lerato Lecturer");

        [SetUp]
        public void SetUp()
        {
            _claims = new Mock<IClaimsService>();
            _documents = new Mock<IDocumentService>();
            _approval = new Mock<IClaimApprovalService>();
            _validation = new Mock<IClaimValidationService>();
            _userManager = TestHelpers.CreateUserManager();
            _userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(_lecturer);

            _controller = new ClaimController(_claims.Object, NullLogger<ClaimController>.Instance, _documents.Object,
                _approval.Object, _validation.Object, _userManager.Object);
            TestHelpers.SignIn(_controller, _lecturer.Id, "Lecturer");
        }

        [TearDown]
        public void TearDown() => _controller.Dispose();

        private static ClaimModel ValidClaim() => new()
        {
            HoursWorked = 10,
            HourlyRate = 300,
            ModuleCode = "PROG6212",
            LecturerDepartment = "IT",
            AdditionalNotes = "Lectures and marking for October"
        };

        [Test]
        public async Task Submit_UsesTheSignedInLecturerNotPostedValues()
        {
            var posted = ValidClaim();
            posted.LecturerId = "someone-else";
            posted.LecturerName = "Someone Else";
            posted.TotalAmount = 1; // tampered total
            ClaimModel? saved = null;
            _documents.Setup(d => d.Validate(It.IsAny<IFormFile>())).Returns((string?)null);
            _documents.Setup(d => d.SaveDocumentAsync(It.IsAny<IFormFile>())).ReturnsAsync("stored.pdf");
            _validation.Setup(v => v.ValidateClaimAsync(It.IsAny<ClaimModel>())).ReturnsAsync((true, Array.Empty<string>()));
            _approval.Setup(a => a.GetInitialStatusAsync(3000m)).ReturnsAsync(ClaimStatus.Approved);
            _claims.Setup(c => c.CreateClaimAsync(It.IsAny<ClaimModel>()))
                .Callback<ClaimModel>(c => saved = c)
                .ReturnsAsync((ClaimModel c) => c);

            var result = await _controller.Submit(posted, TestHelpers.File("timesheet.pdf", 100));

            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
            Assert.That(saved, Is.Not.Null);
            Assert.That(saved!.LecturerId, Is.EqualTo(_lecturer.Id));
            Assert.That(saved.UserId, Is.EqualTo(_lecturer.Id));
            Assert.That(saved.LecturerName, Is.EqualTo("Lerato Lecturer"));
            Assert.That(saved.TotalAmount, Is.EqualTo(3000m));
            Assert.That(saved.Status, Is.EqualTo(ClaimStatus.Approved));
            Assert.That(saved.ReviewerId, Is.Null, "auto-approval must not set a reviewer foreign key");
            Assert.That(saved.SupportingDocumentPath, Is.EqualTo("stored.pdf"));
        }

        [Test]
        public async Task Submit_InvalidDocumentReturnsFormWithoutSaving()
        {
            _documents.Setup(d => d.Validate(It.IsAny<IFormFile>())).Returns("Only .pdf files are allowed.");

            var result = await _controller.Submit(ValidClaim(), TestHelpers.File("virus.exe", 100));

            Assert.That(result, Is.TypeOf<ViewResult>());
            Assert.That(_controller.ModelState.IsValid, Is.False);
            _claims.Verify(c => c.CreateClaimAsync(It.IsAny<ClaimModel>()), Times.Never);
            _documents.Verify(d => d.SaveDocumentAsync(It.IsAny<IFormFile>()), Times.Never);
        }

        [Test]
        public async Task Submit_BusinessRuleFailureReturnsFormWithMessages()
        {
            _documents.Setup(d => d.Validate(It.IsAny<IFormFile>())).Returns((string?)null);
            _validation.Setup(v => v.ValidateClaimAsync(It.IsAny<ClaimModel>()))
                .ReturnsAsync((false, new[] { "You must wait 24 hours between claim submissions." }));

            var result = await _controller.Submit(ValidClaim(), TestHelpers.File("timesheet.pdf", 100));

            Assert.That(result, Is.TypeOf<ViewResult>());
            Assert.That(_controller.ModelState[string.Empty]!.Errors.Single().ErrorMessage, Does.Contain("24 hours"));
            _claims.Verify(c => c.CreateClaimAsync(It.IsAny<ClaimModel>()), Times.Never);
        }

        [Test]
        public async Task Dashboard_ShowsOnlyTheLecturersOwnClaims()
        {
            _claims.Setup(c => c.GetAllClaimsAsync()).ReturnsAsync(new[]
            {
                new ClaimModel { Id = 1, LecturerId = _lecturer.Id, LecturerName = "Lerato Lecturer" },
                new ClaimModel { Id = 2, LecturerId = "other", LecturerName = "Lerato Lecturer" }
            });

            var result = (ViewResult)await _controller.Dashboard();

            var model = (DashboardViewModel)result.Model!;
            Assert.That(model.Claims.Select(c => c.Id), Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public async Task DownloadDocument_LecturerCannotDownloadSomeoneElsesDocument()
        {
            _claims.Setup(c => c.GetClaimByIdAsync(5))
                .ReturnsAsync(new ClaimModel { Id = 5, LecturerId = "other", SupportingDocumentPath = "doc.pdf" });

            var result = await _controller.DownloadDocument(5);

            Assert.That(result, Is.TypeOf<ForbidResult>());
            _documents.Verify(d => d.GetDocumentPath(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task DownloadDocument_ReviewerCanDownloadAnyDocument()
        {
            var file = Path.GetTempFileName();
            try
            {
                TestHelpers.SignIn(_controller, "reviewer", "ProgrammeCoordinator");
                _claims.Setup(c => c.GetClaimByIdAsync(5))
                    .ReturnsAsync(new ClaimModel { Id = 5, LecturerId = "other", SupportingDocumentPath = "doc.pdf" });
                _documents.Setup(d => d.GetDocumentPath("doc.pdf")).Returns(file);

                var result = await _controller.DownloadDocument(5);

                Assert.That(result, Is.TypeOf<PhysicalFileResult>());
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Test]
        public async Task ConfirmReject_UsesTheApprovalService()
        {
            _approval.Setup(a => a.RejectAsync(5, _lecturer, "Wrong module"))
                .ReturnsAsync(ReviewResult.Success("Claim 5 rejected."));

            var result = await _controller.ConfirmReject(5, "Wrong module");

            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
            Assert.That(_controller.TempData["SuccessMessage"], Is.EqualTo("Claim 5 rejected."));
        }
    }
}
