using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using CMCS.Models;

namespace CMCS.Services
{
    public interface IIdentityService
    {
        // User Token Management
        Task<string> GenerateEmailConfirmationTokenAsync(ApplicationUser user);
        Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user);
        Task<bool> ValidatePasswordResetTokenAsync(ApplicationUser user, string token);

        // External Login Management
        Task<IdentityResult> AddLoginAsync(ApplicationUser user, UserLoginInfo login);
        Task<IdentityResult> RemoveLoginAsync(ApplicationUser user, string loginProvider, string providerKey);
        Task<IList<UserLoginInfo>> GetLoginsAsync(ApplicationUser user);

        // User Claims Management
        Task<IdentityResult> AddClaimAsync(ApplicationUser user, System.Security.Claims.Claim claim);
        Task<IdentityResult> AddClaimsAsync(ApplicationUser user, IEnumerable<System.Security.Claims.Claim> claims);
        Task<IdentityResult> ReplaceClaimAsync(ApplicationUser user, System.Security.Claims.Claim claim, System.Security.Claims.Claim newClaim);
        Task<IdentityResult> RemoveClaimAsync(ApplicationUser user, System.Security.Claims.Claim claim);
        Task<IList<System.Security.Claims.Claim>> GetClaimsAsync(ApplicationUser user);

        // Role Claims Management
        Task<IdentityResult> AddClaimToRoleAsync(IdentityRole role, System.Security.Claims.Claim claim);
        Task<IdentityResult> RemoveClaimFromRoleAsync(IdentityRole role, System.Security.Claims.Claim claim);
        Task<IList<System.Security.Claims.Claim>> GetRoleClaimsAsync(IdentityRole role);
    }
}
