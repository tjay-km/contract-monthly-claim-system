using CMCS.Models;
using CMCS.Models.Enums;
using CMCS.Services;
using Moq;
using NUnit.Framework;
using ClaimModel = CMCS.Models.Claim;

namespace CMCS.Tests
{
    [TestFixture]
    public class ClaimApprovalServiceTests
    {
        private Mock<IClaimsService> _claims = null!;
        private Mock<IAutoApprovalConfigService> _config = null!;
        private Mock<IClaimAuditService> _audit = null!;
        private ClaimApprovalService _service = null!;
        private readonly ApplicationUser _reviewer = TestHelpers.User("reviewer", "Rita Reviewer");

        [SetUp]
        public void SetUp()
        {
            _claims = new Mock<IClaimsService>();
            _config = new Mock<IAutoApprovalConfigService>();
            _audit = new Mock<IClaimAuditService>();
            _service = new ClaimApprovalService(_claims.Object, _config.Object, _audit.Object);
        }

        private ClaimModel GivenClaim(decimal amount, ClaimStatus status = ClaimStatus.Pending)
        {
            var claim = new ClaimModel { Id = 7, TotalAmount = amount, Status = status, LecturerId = "lecturer" };
            _claims.Setup(s => s.GetClaimByIdAsync(7)).ReturnsAsync(claim);
            return claim;
        }

        [TestCase(true, ClaimStatus.Approved)]
        [TestCase(false, ClaimStatus.Pending)]
        public async Task GetInitialStatus_FollowsAutoApprovalConfig(bool eligible, ClaimStatus expected)
        {
            _config.Setup(c => c.IsEligibleForAutoApprovalAsync(1000m)).ReturnsAsync(eligible);

            Assert.That(await _service.GetInitialStatusAsync(1000m), Is.EqualTo(expected));
        }

        [Test]
        public async Task Approve_RecordsReviewerAndAudits()
        {
            var claim = GivenClaim(4000m);

            var result = await _service.ApproveAsync(7, _reviewer, isAcademicManager: false);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(claim.Status, Is.EqualTo(ClaimStatus.Approved));
            Assert.That(claim.ReviewerId, Is.EqualTo("reviewer"));
            Assert.That(claim.ReviewedBy, Is.EqualTo("Rita Reviewer"));
            Assert.That(claim.DateReviewed, Is.Not.Null);
            _claims.Verify(s => s.UpdateClaimAsync(claim), Times.Once);
            _audit.Verify(a => a.LogAuditAsync(7, "Approved", "Rita Reviewer", It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task Approve_CoordinatorCannotApproveAboveTheLimit()
        {
            var claim = GivenClaim(ClaimApprovalService.CoordinatorApprovalLimit + 0.01m);

            var result = await _service.ApproveAsync(7, _reviewer, isAcademicManager: false);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(claim.Status, Is.EqualTo(ClaimStatus.Pending));
            _claims.Verify(s => s.UpdateClaimAsync(It.IsAny<ClaimModel>()), Times.Never);
        }

        [Test]
        public async Task Approve_AcademicManagerCanApproveLargeClaims()
        {
            var claim = GivenClaim(50000m);

            var result = await _service.ApproveAsync(7, _reviewer, isAcademicManager: true);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(claim.Status, Is.EqualTo(ClaimStatus.Approved));
        }

        [TestCase(ClaimStatus.Approved)]
        [TestCase(ClaimStatus.Rejected)]
        public async Task Approve_RefusesClaimsThatWereAlreadyReviewed(ClaimStatus status)
        {
            GivenClaim(100m, status);

            var result = await _service.ApproveAsync(7, _reviewer, isAcademicManager: true);

            Assert.That(result.Succeeded, Is.False);
            _claims.Verify(s => s.UpdateClaimAsync(It.IsAny<ClaimModel>()), Times.Never);
        }

        [Test]
        public async Task Approve_UnknownClaimFails()
        {
            var result = await _service.ApproveAsync(99, _reviewer, isAcademicManager: true);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Message, Is.EqualTo("Claim not found."));
        }

        [TestCase(null)]
        [TestCase("   ")]
        public async Task Reject_RequiresAReason(string? reason)
        {
            GivenClaim(100m);

            var result = await _service.RejectAsync(7, _reviewer, reason!);

            Assert.That(result.Succeeded, Is.False);
            _claims.Verify(s => s.UpdateClaimAsync(It.IsAny<ClaimModel>()), Times.Never);
        }

        [Test]
        public async Task Reject_StoresTrimmedReasonAndReviewer()
        {
            var claim = GivenClaim(100m);

            var result = await _service.RejectAsync(7, _reviewer, "  Hours do not match the timetable  ");

            Assert.That(result.Succeeded, Is.True);
            Assert.That(claim.Status, Is.EqualTo(ClaimStatus.Rejected));
            Assert.That(claim.RejectionReason, Is.EqualTo("Hours do not match the timetable"));
            Assert.That(claim.ReviewerId, Is.EqualTo("reviewer"));
            _audit.Verify(a => a.LogAuditAsync(7, "Rejected", "Rita Reviewer", It.IsAny<string>()), Times.Once);
        }
    }
}
