using System;
using System.Threading.Tasks;
using CMCS.Models;
using Microsoft.AspNetCore.Identity;
using ClaimModel = CMCS.Models.Claim;
using CMCS.Models.Enums;

namespace CMCS.Services
{
    public interface IClaimApprovalService
    {
        Task<bool> ProcessClaimAsync(ClaimModel claim, string reviewerId);
        Task<bool> ApproveClaimAsync(int claimId, string reviewerId);
        Task<bool> RejectClaimAsync(int claimId, string reviewerId, string rejectionReason);
    }

    public class ClaimApprovalService : IClaimApprovalService
    {
        private readonly IClaimValidationService _validationService;
        private readonly IClaimsService _claimsService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IClaimAuditService _auditService;
        private readonly IClaimReportService _reportService;

        public ClaimApprovalService(
            IClaimValidationService validationService,
            IClaimsService claimsService,
            UserManager<ApplicationUser> userManager,
            IClaimAuditService auditService,
            IClaimReportService reportService)
        {
            _validationService = validationService;
            _claimsService = claimsService;
            _userManager = userManager;
            _auditService = auditService;
            _reportService = reportService;
        }

        public async Task<bool> ProcessClaimAsync(ClaimModel claim, string reviewerId)
        {
            // First, validate the claim
            var (isValid, validationMessages) = await _validationService.ValidateClaimAsync(claim);
            if (!isValid)
            {
                return false;
            }

            // Check if claim has already been reviewed
            if (claim.Status == ClaimStatus.Approved || claim.Status == ClaimStatus.Rejected)
            {
                throw new InvalidOperationException("Claim has already been processed.");
            }

            // Get reviewer's role
            var reviewer = await _userManager.FindByIdAsync(reviewerId);
            if (reviewer == null)
            {
                return false;
            }

            var roles = await _userManager.GetRolesAsync(reviewer);

            // Get auto-approval configuration
            var config = await _reportService.GetAutoApprovalConfigAsync();

            // Check if auto-approval is enabled and claim amount is within limit
            if (config.EnableAutoApproval && 
                claim.TotalAmount <= config.MaxAutoApprovalAmount && 
                (roles.Contains("ProgrammeCoordinator") || roles.Contains("AcademicManager")))
            {
                claim.Status = ClaimStatus.Approved;
                claim.DateReviewed = DateTime.Now;
                claim.ReviewerId = reviewerId;
                await _claimsService.UpdateClaimAsync(claim);

                // Log the auto-approval
                await _auditService.LogAuditAsync(
                    claim.Id,
                    "Auto-Approved",
                    reviewer.UserName,
                    $"Claim auto-approved. Amount: R{claim.TotalAmount:N2}");

                return true;
            }
            // Claims over R5000 need Academic Manager approval
            else if (claim.TotalAmount > 5000)
            {
                claim.Status = ClaimStatus.PendingAcademicManagerApproval;
                await _claimsService.UpdateClaimAsync(claim);
            }
            else
            {
                // Set to pending for manual review
                claim.Status = ClaimStatus.PendingReview;
                await _claimsService.UpdateClaimAsync(claim);
            }

            return true;
        }

        public async Task<bool> ApproveClaimAsync(int claimId, string reviewerId)
        {
            var claim = await _claimsService.GetClaimByIdAsync(claimId);
            if (claim == null) return false;

            claim.Status = ClaimStatus.Approved;
            claim.DateReviewed = DateTime.Now;
            claim.ReviewerId = reviewerId;

            await _claimsService.UpdateClaimAsync(claim);
            return true;
        }

        public async Task<bool> RejectClaimAsync(int claimId, string reviewerId, string rejectionReason)
        {
            var claim = await _claimsService.GetClaimByIdAsync(claimId);
            if (claim == null) return false;

            claim.Status = ClaimStatus.Rejected;
            claim.DateReviewed = DateTime.Now;
            claim.ReviewerId = reviewerId;
            claim.RejectionReason = rejectionReason;

            await _claimsService.UpdateClaimAsync(claim);
            return true;
        }
    }
}
