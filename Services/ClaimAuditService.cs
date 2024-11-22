using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using CMCS.Models;
using CMCS.Data;

namespace CMCS.Services
{
    public interface IClaimAuditService
    {
        Task LogAuditAsync(int? claimId, string action, string performedBy, string details);
        Task<IEnumerable<ClaimAudit>> GetAuditTrailForClaimAsync(int claimId);
        Task<IEnumerable<ClaimAudit>> GetRecentAuditsAsync(int count = 50);
    }

    public class ClaimAuditService : IClaimAuditService
    {
        private readonly ApplicationDbContext _context;

        public ClaimAuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAuditAsync(int? claimId, string action, string performedBy, string details)
        {
            // Special handling for report generation and other system-wide actions
            if (claimId == null && action.Contains("Report"))
            {
                // Log a system-wide audit without requiring a claim
                var systemAudit = new ClaimAudit
                {
                    ClaimId = claimId, 
                    Action = action,
                    PerformedBy = performedBy ?? "System",
                    Timestamp = DateTime.Now,
                    Details = details
                };

                try 
                {
                    _context.ClaimAudits.Add(systemAudit);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error logging system audit: {ex.Message}");
                    Console.WriteLine($"Error logging system audit: {ex.Message}");
                }
                return;
            }

            // Original claim-specific audit logging
            if (claimId == null)
            {
                System.Diagnostics.Debug.WriteLine($"Skipping audit log for invalid claimId: {claimId}");
                Console.WriteLine($"Skipping audit log for invalid claimId: {claimId}");
                return;
            }

            var audit = new ClaimAudit
            {
                ClaimId = claimId,
                Action = action,
                PerformedBy = performedBy ?? "System",
                Timestamp = DateTime.Now,
                Details = details
            };

            try 
            {
                _context.ClaimAudits.Add(audit);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error logging audit: {ex.Message}");
                Console.WriteLine($"Error logging audit: {ex.Message}");
                
                // Optional: Log to file
                try 
                {
                    var logPath = System.IO.Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory, 
                        "audit_log_errors.txt"
                    );
                    await System.IO.File.AppendAllTextAsync(logPath, 
                        $"{DateTime.Now}: Audit Logging Error\n" +
                        $"ClaimId: {claimId}\n" +
                        $"Action: {action}\n" +
                        $"Error: {ex.Message}\n\n");
                }
                catch { }
            }
        }

        public async Task<IEnumerable<ClaimAudit>> GetAuditTrailForClaimAsync(int claimId)
        {
            return await _context.ClaimAudits
                .Where(a => a.ClaimId == claimId)
                .OrderByDescending(a => a.Timestamp)
                .Include(a => a.Claim)
                .ToListAsync();
        }

        public async Task<IEnumerable<ClaimAudit>> GetRecentAuditsAsync(int count = 50)
        {
            return await _context.ClaimAudits
                .OrderByDescending(a => a.Timestamp)
                .Include(a => a.Claim)
                .Take(count)
                .ToListAsync();
        }
    }
}
