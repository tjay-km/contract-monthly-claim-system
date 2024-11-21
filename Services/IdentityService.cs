using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using CMCS.Models;

namespace CMCS.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public IdentityService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // User Token Management
        public async Task<string> GenerateEmailConfirmationTokenAsync(ApplicationUser user)
        {
            return await _userManager.GenerateEmailConfirmationTokenAsync(user);
        }

        public async Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user)
        {
            return await _userManager.GeneratePasswordResetTokenAsync(user);
        }

        public async Task<bool> ValidatePasswordResetTokenAsync(ApplicationUser user, string token)
        {
            return await _userManager.VerifyUserTokenAsync(user, 
                _userManager.Options.Tokens.PasswordResetTokenProvider,
                UserManager<ApplicationUser>.ResetPasswordTokenPurpose, 
                token);
        }

        // External Login Management
        public async Task<IdentityResult> AddLoginAsync(ApplicationUser user, UserLoginInfo login)
        {
            return await _userManager.AddLoginAsync(user, login);
        }

        public async Task<IdentityResult> RemoveLoginAsync(ApplicationUser user, string loginProvider, string providerKey)
        {
            return await _userManager.RemoveLoginAsync(user, loginProvider, providerKey);
        }

        public async Task<IList<UserLoginInfo>> GetLoginsAsync(ApplicationUser user)
        {
            return await _userManager.GetLoginsAsync(user);
        }

        // User Claims Management
        public async Task<IdentityResult> AddClaimAsync(ApplicationUser user, System.Security.Claims.Claim claim)
        {
            return await _userManager.AddClaimAsync(user, claim);
        }

        public async Task<IdentityResult> AddClaimsAsync(ApplicationUser user, IEnumerable<System.Security.Claims.Claim> claims)
        {
            return await _userManager.AddClaimsAsync(user, claims);
        }

        public async Task<IdentityResult> ReplaceClaimAsync(ApplicationUser user, System.Security.Claims.Claim claim, System.Security.Claims.Claim newClaim)
        {
            return await _userManager.ReplaceClaimAsync(user, claim, newClaim);
        }

        public async Task<IdentityResult> RemoveClaimAsync(ApplicationUser user, System.Security.Claims.Claim claim)
        {
            return await _userManager.RemoveClaimAsync(user, claim);
        }

        public async Task<IList<System.Security.Claims.Claim>> GetClaimsAsync(ApplicationUser user)
        {
            return await _userManager.GetClaimsAsync(user);
        }

        // Role Claims Management
        public async Task<IdentityResult> AddClaimToRoleAsync(IdentityRole role, System.Security.Claims.Claim claim)
        {
            return await _roleManager.AddClaimAsync(role, claim);
        }

        public async Task<IdentityResult> RemoveClaimFromRoleAsync(IdentityRole role, System.Security.Claims.Claim claim)
        {
            return await _roleManager.RemoveClaimAsync(role, claim);
        }

        public async Task<IList<System.Security.Claims.Claim>> GetRoleClaimsAsync(IdentityRole role)
        {
            return await _roleManager.GetClaimsAsync(role);
        }
    }
}
