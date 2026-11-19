using System;
using System.Linq;
using System.Threading.Tasks;
using CMCS.Data;
using CMCS.Models;
using Microsoft.EntityFrameworkCore;

namespace CMCS.Services
{
    public class AutoApprovalConfigService : IAutoApprovalConfigService
    {
        private readonly ApplicationDbContext _context;

        public AutoApprovalConfigService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AutoApprovalConfig> GetCurrentConfigAsync()
        {
            var config = await _context.AutoApprovalConfigs
                .AsNoTracking()
                .OrderByDescending(c => c.LastModifiedDate)
                .ThenByDescending(c => c.Id)
                .FirstOrDefaultAsync();

            return config ?? new AutoApprovalConfig
            {
                LastModifiedBy = "System",
                Description = "Default auto-approval configuration"
            };
        }

        public async Task UpdateConfigAsync(AutoApprovalConfig config, string modifiedBy)
        {
            _context.AutoApprovalConfigs.Add(new AutoApprovalConfig
            {
                EnableAutoApproval = config.EnableAutoApproval,
                MaxAutoApprovalAmount = config.MaxAutoApprovalAmount,
                Description = config.Description ?? string.Empty,
                LastModifiedBy = modifiedBy,
                LastModifiedDate = DateTime.Now
            });
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsEligibleForAutoApprovalAsync(decimal amount)
        {
            var config = await GetCurrentConfigAsync();
            return config.EnableAutoApproval && amount <= config.MaxAutoApprovalAmount;
        }
    }
}
