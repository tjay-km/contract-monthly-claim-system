using System;
using System.Threading.Tasks;
using CMCS.Models;

namespace CMCS.Services
{
    public interface IAutoApprovalConfigService
    {
        Task<AutoApprovalConfig> GetCurrentConfigAsync();
        Task UpdateConfigAsync(AutoApprovalConfig config, string modifiedBy);
        Task<bool> IsEligibleForAutoApprovalAsync(decimal amount);
    }
}
