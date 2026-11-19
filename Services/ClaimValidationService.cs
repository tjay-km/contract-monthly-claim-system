using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMCS.Data;
using CMCS.Models;
using Microsoft.EntityFrameworkCore;

namespace CMCS.Services
{
    public interface IClaimValidationService
    {
        /// <summary>Business rules checked when a lecturer submits a claim.</summary>
        Task<(bool IsValid, string[] ValidationMessages)> ValidateClaimAsync(Claim claim);
    }

    public class ClaimValidationService : IClaimValidationService
    {
        public const decimal MaxHoursPerClaim = 195;
        public const decimal MaxHourlyRate = 20000;
        public const int MaxClaimsPerMonth = 4;
        public static readonly TimeSpan MinTimeBetweenClaims = TimeSpan.FromHours(24);

        private readonly ApplicationDbContext _context;

        public ClaimValidationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(bool IsValid, string[] ValidationMessages)> ValidateClaimAsync(Claim claim)
        {
            var messages = new List<string>();

            if (string.IsNullOrWhiteSpace(claim.ModuleCode))
                messages.Add("Module code is required.");

            if (claim.HoursWorked <= 0)
                messages.Add("Hours worked must be greater than 0.");
            else if (claim.HoursWorked > MaxHoursPerClaim)
                messages.Add($"Hours worked cannot exceed {MaxHoursPerClaim} hours.");

            if (claim.HourlyRate <= 0)
                messages.Add("Hourly rate must be greater than 0.");
            else if (claim.HourlyRate > MaxHourlyRate)
                messages.Add($"Hourly rate cannot exceed R{MaxHourlyRate:N0}.");

            // The total is always derived from hours and rate, never trusted from the form.
            claim.TotalAmount = claim.HoursWorked * claim.HourlyRate;

            var lecturerExists = await _context.Users.AnyAsync(u => u.Id == claim.LecturerId);
            if (!lecturerExists)
            {
                messages.Add("Invalid lecturer.");
                return (false, messages.ToArray());
            }

            var now = DateTime.Now;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var lecturerClaims = _context.Claims.Where(c => c.LecturerId == claim.LecturerId);

            if (await lecturerClaims.CountAsync(c => c.SubmissionDate >= monthStart) >= MaxClaimsPerMonth)
                messages.Add($"You can submit at most {MaxClaimsPerMonth} claims per month.");

            var lastSubmission = await lecturerClaims
                .OrderByDescending(c => c.SubmissionDate)
                .Select(c => (DateTime?)c.SubmissionDate)
                .FirstOrDefaultAsync();
            if (lastSubmission.HasValue && now - lastSubmission.Value < MinTimeBetweenClaims)
                messages.Add("You must wait 24 hours between claim submissions.");

            return (messages.Count == 0, messages.ToArray());
        }
    }
}
