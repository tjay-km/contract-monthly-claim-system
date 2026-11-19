using System.Threading.Tasks;
using CMCS.Models;

namespace CMCS.Services
{
    public interface IAutoApprovalConfigService
    {
        /// <summary>The most recently saved configuration, or the defaults if none has been saved.</summary>
        Task<AutoApprovalConfig> GetCurrentConfigAsync();

        /// <summary>Saves a new configuration version (older versions are kept as history).</summary>
        Task UpdateConfigAsync(AutoApprovalConfig config, string modifiedBy);

        Task<bool> IsEligibleForAutoApprovalAsync(decimal amount);
    }
}
