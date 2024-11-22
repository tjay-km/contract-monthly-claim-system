using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using CMCS.Models;
using CMCS.Data;
using System.IO;

namespace CMCS.Services
{
    public interface IClaimValidationService
    {
        Task<(bool IsValid, string[] ValidationMessages)> ValidateClaimAsync(Claim claim);
    }

    public class ClaimValidationService : IClaimValidationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAutoApprovalConfigService _autoApprovalConfigService;

        public ClaimValidationService(
            ApplicationDbContext context,
            IAutoApprovalConfigService autoApprovalConfigService)
        {
            _context = context;
            _autoApprovalConfigService = autoApprovalConfigService;
        }

        public async Task<(bool IsValid, string[] ValidationMessages)> ValidateClaimAsync(Claim claim)
        {
            var validationMessages = new List<string>();

            // Validate submission date
            if (claim.SubmissionDate > DateTime.Now)
            {
                validationMessages.Add("Submission date cannot be in the future");
            }

            var maxPastDate = DateTime.Now.AddMonths(-3);
            if (claim.SubmissionDate < maxPastDate)
            {
                validationMessages.Add("Claims cannot be submitted for work done more than 3 months ago");
            }

            // Validate module/course information
            if (string.IsNullOrWhiteSpace(claim.ModuleCode))
            {
                validationMessages.Add("Module code is required");
            }

            // Validate hours and rate
            if (claim.HoursWorked <= 0)
            {
                validationMessages.Add("Hours worked must be greater than 0");
            }
            else if (claim.HoursWorked > 195)
            {
                validationMessages.Add("Hours worked cannot exceed 195 hours");
            }

            if (claim.HourlyRate <= 0)
            {
                validationMessages.Add("Hourly rate must be greater than 0");
            }
            else if (claim.HourlyRate > 20000)
            {
                validationMessages.Add("Hourly rate cannot exceed 20000");
            }

            // Calculate and validate total amount
            decimal calculatedTotal = claim.HoursWorked * claim.HourlyRate;
            if (claim.TotalAmount != calculatedTotal)
            {
                claim.TotalAmount = calculatedTotal; // Auto-correct the total amount
            }

            if (calculatedTotal <= 0 || calculatedTotal > 3900000) // 195 hours * 20000 rate = 3,900,000 max
            {
                validationMessages.Add("Total amount is outside the valid range (must be between 0.01 and 3,900,000)");
            }

            // Validate supporting document
            if (claim.SupportingDocument == null && string.IsNullOrEmpty(claim.SupportingDocumentPath))
            {
                validationMessages.Add("Supporting document is required");
            }
            else if (!string.IsNullOrEmpty(claim.SupportingDocumentPath) && !File.Exists(claim.SupportingDocumentPath))
            {
                validationMessages.Add("Supporting document file not found");
            }

            // Validate lecturer eligibility
            var lecturer = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == claim.LecturerId);
            
            if (lecturer == null)
            {
                validationMessages.Add("Invalid lecturer ID");
            }
            else
            {
                var existingClaimsThisMonth = await _context.Claims
                    .Where(c => c.LecturerId == claim.LecturerId &&
                           c.SubmissionDate.Month == DateTime.Now.Month &&
                           c.SubmissionDate.Year == DateTime.Now.Year)
                    .CountAsync();

                if (existingClaimsThisMonth >= 4)
                {
                    validationMessages.Add("Maximum of 4 claims per month allowed");
                }
            }

            // Validate claim submission timing
            var lastClaim = await _context.Claims
                .Where(c => c.LecturerId == claim.LecturerId)
                .OrderByDescending(c => c.SubmissionDate)
                .FirstOrDefaultAsync();

            if (lastClaim != null && (DateTime.Now - lastClaim.SubmissionDate).TotalHours < 24)
            {
                validationMessages.Add("You must wait 24 hours between claim submissions");
            }

            // Validate against auto-approval threshold
            var autoApprovalConfig = await _autoApprovalConfigService.GetCurrentConfigAsync();
            if (autoApprovalConfig != null && autoApprovalConfig.EnableAutoApproval && claim.TotalAmount > autoApprovalConfig.MaxAutoApprovalAmount)
            {
                validationMessages.Add($"Claims exceeding R{autoApprovalConfig.MaxAutoApprovalAmount:N2} require manual approval");
            }

            return (validationMessages.Count == 0, validationMessages.ToArray());
        }
    }
}