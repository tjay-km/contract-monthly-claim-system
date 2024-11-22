using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CMCS.Models;
using CMCS.Data;

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
            var config = await _context.AutoApprovalConfigs.FirstOrDefaultAsync();
            if (config == null)
            {
                config = new AutoApprovalConfig
                {
                    MaxAutoApprovalAmount = 5000m, // Default maximum amount
                    EnableAutoApproval = true,
                    LastModifiedBy = "System",
                    LastModifiedDate = DateTime.Now,
                    Description = "Default auto-approval configuration"
                };
                _context.AutoApprovalConfigs.Add(config);
                await _context.SaveChangesAsync();
            }
            return config;
        }

        public async Task UpdateConfigAsync(AutoApprovalConfig config, string modifiedBy)
        {
            config.LastModifiedBy = modifiedBy;
            config.LastModifiedDate = DateTime.Now;
            _context.AutoApprovalConfigs.Update(config);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsEligibleForAutoApprovalAsync(decimal amount)
        {
            var config = await GetCurrentConfigAsync();
            return config.EnableAutoApproval && amount <= config.MaxAutoApprovalAmount;
        }
    }
}
