using System;
using System.Threading.Tasks;
using CMCS.Models;
using CMCS.Models.Enums;
using ClaimModel = CMCS.Models.Claim;

namespace CMCS.Services
{
    /// <summary>Outcome of an approve or reject request.</summary>
    public record ReviewResult(bool Succeeded, string Message)
    {
        public static ReviewResult Success(string message) => new(true, message);
        public static ReviewResult Failure(string message) => new(false, message);
    }

    public interface IClaimApprovalService
    {
        /// <summary>Approved when auto-approval is on and the amount is within its limit, otherwise Pending.</summary>
        Task<ClaimStatus> GetInitialStatusAsync(decimal totalAmount);

        Task<ReviewResult> ApproveAsync(int claimId, ApplicationUser reviewer, bool isAcademicManager);

        Task<ReviewResult> RejectAsync(int claimId, ApplicationUser reviewer, string reason);
    }

    /// <summary>
    /// The single place where claims change status, so the approval rules and the audit trail
    /// apply no matter which page the reviewer uses.
    /// </summary>
    public class ClaimApprovalService : IClaimApprovalService
    {
        /// <summary>Programme coordinators can approve claims up to this amount; larger claims need an academic manager.</summary>
        public const decimal CoordinatorApprovalLimit = 5000m;

        private readonly IClaimsService _claimsService;
        private readonly IAutoApprovalConfigService _autoApprovalConfig;
        private readonly IClaimAuditService _auditService;

        public ClaimApprovalService(
            IClaimsService claimsService,
            IAutoApprovalConfigService autoApprovalConfig,
            IClaimAuditService auditService)
        {
            _claimsService = claimsService;
            _autoApprovalConfig = autoApprovalConfig;
            _auditService = auditService;
        }

        public static bool IsPending(ClaimStatus status) =>
            status is ClaimStatus.Pending or ClaimStatus.PendingReview or ClaimStatus.PendingAcademicManagerApproval;

        public async Task<ClaimStatus> GetInitialStatusAsync(decimal totalAmount) =>
            await _autoApprovalConfig.IsEligibleForAutoApprovalAsync(totalAmount)
                ? ClaimStatus.Approved
                : ClaimStatus.Pending;

        public async Task<ReviewResult> ApproveAsync(int claimId, ApplicationUser reviewer, bool isAcademicManager)
        {
            var (claim, error) = await LoadPendingClaimAsync(claimId);
            if (claim == null)
                return ReviewResult.Failure(error);

            if (!isAcademicManager && claim.TotalAmount > CoordinatorApprovalLimit)
                return ReviewResult.Failure(
                    $"Claims over R{CoordinatorApprovalLimit:N0} must be approved by an Academic Manager.");

            claim.Status = ClaimStatus.Approved;
            claim.ApprovalComments = "Approved by reviewer";
            MarkReviewed(claim, reviewer);
            await _claimsService.UpdateClaimAsync(claim);

            await _auditService.LogAuditAsync(claim.Id, "Approved", ReviewerName(reviewer),
                $"Approved claim of R{claim.TotalAmount:N2}");
            return ReviewResult.Success($"Claim {claim.Id} approved.");
        }

        public async Task<ReviewResult> RejectAsync(int claimId, ApplicationUser reviewer, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return ReviewResult.Failure("A rejection reason is required.");

            var (claim, error) = await LoadPendingClaimAsync(claimId);
            if (claim == null)
                return ReviewResult.Failure(error);

            claim.Status = ClaimStatus.Rejected;
            claim.RejectionReason = reason.Trim();
            MarkReviewed(claim, reviewer);
            await _claimsService.UpdateClaimAsync(claim);

            await _auditService.LogAuditAsync(claim.Id, "Rejected", ReviewerName(reviewer),
                $"Rejected claim of R{claim.TotalAmount:N2}: {claim.RejectionReason}");
            return ReviewResult.Success($"Claim {claim.Id} rejected.");
        }

        private async Task<(ClaimModel? Claim, string Error)> LoadPendingClaimAsync(int claimId)
        {
            var claim = await _claimsService.GetClaimByIdAsync(claimId);
            if (claim == null)
                return (null, "Claim not found.");
            if (!IsPending(claim.Status))
                return (null, "This claim has already been reviewed.");
            return (claim, string.Empty);
        }

        private static void MarkReviewed(ClaimModel claim, ApplicationUser reviewer)
        {
            claim.ReviewerId = reviewer.Id;
            claim.ReviewedBy = ReviewerName(reviewer);
            claim.DateReviewed = DateTime.Now;
        }

        private static string ReviewerName(ApplicationUser reviewer) =>
            string.IsNullOrWhiteSpace(reviewer.Name) ? reviewer.UserName ?? reviewer.Id : reviewer.Name;
    }
}
