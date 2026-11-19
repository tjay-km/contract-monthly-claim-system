using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMCS.Data;
using CMCS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CMCS.Services
{
    public interface IClaimAuditService
    {
        /// <summary>Records an action. Use a null claimId for actions that aren't about one claim.</summary>
        Task LogAuditAsync(int? claimId, string action, string performedBy, string details);
        Task<IEnumerable<ClaimAudit>> GetRecentAuditsAsync(int count = 50);
    }

    public class ClaimAuditService : IClaimAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ClaimAuditService> _logger;

        public ClaimAuditService(ApplicationDbContext context, ILogger<ClaimAuditService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogAuditAsync(int? claimId, string action, string performedBy, string details)
        {
            _context.ClaimAudits.Add(new ClaimAudit
            {
                ClaimId = claimId,
                Action = action,
                PerformedBy = string.IsNullOrWhiteSpace(performedBy) ? "System" : performedBy,
                Timestamp = DateTime.Now,
                Details = details
            });

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Auditing must never break the action being audited.
                _logger.LogError(ex, "Could not write audit entry {Action} for claim {ClaimId}", action, claimId);
            }
        }

        public async Task<IEnumerable<ClaimAudit>> GetRecentAuditsAsync(int count = 50)
        {
            return await _context.ClaimAudits
                .AsNoTracking()
                .Include(a => a.Claim)
                .OrderByDescending(a => a.Timestamp)
                .Take(count)
                .ToListAsync();
        }
    }
}
