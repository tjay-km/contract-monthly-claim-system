using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using CMCS.Models;
using System.Linq;

namespace CMCS.Data
{
    public static class SeedData
    {
        public static async Task InitializeRolesAndClaims(
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager)
        {
            // Create roles if they don't exist
            string[] roles = { "Lecturer", "ProgrammeCoordinator", "AcademicManager" };
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Add claims to roles
            var lecturerRole = await roleManager.FindByNameAsync("Lecturer");
            var coordinatorRole = await roleManager.FindByNameAsync("ProgrammeCoordinator");
            var managerRole = await roleManager.FindByNameAsync("AcademicManager");

            // Lecturer claims
            if (!await roleManager.GetClaimsAsync(lecturerRole).ContinueWith(t => t.Result.Any(c => c.Type == "Permission")))
            {
                await roleManager.AddClaimAsync(lecturerRole, new System.Security.Claims.Claim("Permission", "SubmitClaim"));
                await roleManager.AddClaimAsync(lecturerRole, new System.Security.Claims.Claim("Permission", "ViewOwnClaims"));
            }

            // Programme Coordinator claims
            if (!await roleManager.GetClaimsAsync(coordinatorRole).ContinueWith(t => t.Result.Any(c => c.Type == "Permission")))
            {
                await roleManager.AddClaimAsync(coordinatorRole, new System.Security.Claims.Claim("Permission", "ReviewClaims"));
                await roleManager.AddClaimAsync(coordinatorRole, new System.Security.Claims.Claim("Permission", "ApproveClaims"));
                await roleManager.AddClaimAsync(coordinatorRole, new System.Security.Claims.Claim("Permission", "RejectClaims"));
                await roleManager.AddClaimAsync(coordinatorRole, new System.Security.Claims.Claim("MaxApprovalAmount", "5000"));
            }

            // Academic Manager claims
            if (!await roleManager.GetClaimsAsync(managerRole).ContinueWith(t => t.Result.Any(c => c.Type == "Permission")))
            {
                await roleManager.AddClaimAsync(managerRole, new System.Security.Claims.Claim("Permission", "ReviewClaims"));
                await roleManager.AddClaimAsync(managerRole, new System.Security.Claims.Claim("Permission", "ApproveClaims"));
                await roleManager.AddClaimAsync(managerRole, new System.Security.Claims.Claim("Permission", "RejectClaims"));
                await roleManager.AddClaimAsync(managerRole, new System.Security.Claims.Claim("Permission", "ViewAllClaims"));
                await roleManager.AddClaimAsync(managerRole, new System.Security.Claims.Claim("MaxApprovalAmount", "20000"));
            }

            // Add default test users if they don't exist
            var testUsers = new[]
            {
                new { Email = "lecturer@test.com", Role = "Lecturer", Department = "Computer Science" },
                new { Email = "coordinator@test.com", Role = "ProgrammeCoordinator", Department = "Information Technology" },
                new { Email = "manager@test.com", Role = "AcademicManager", Department = "Academic Affairs" }
            };

            foreach (var user in testUsers)
            {
                var existingUser = await userManager.FindByEmailAsync(user.Email);
                if (existingUser == null)
                {
                    var newUser = new ApplicationUser
                    {
                        UserName = user.Email,
                        Email = user.Email,
                        EmailConfirmed = true,
                        Department = user.Department,
                        FirstName = user.Role.Replace("Programme", "").Replace("Academic", ""),
                        LastName = "User",
                        Name = $"{user.Role.Replace("Programme", "").Replace("Academic", "")} User"
                    };

                    var result = await userManager.CreateAsync(newUser, "Test@123");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(newUser, user.Role);

                        // Add user-specific claims
                        await userManager.AddClaimAsync(newUser, new System.Security.Claims.Claim("Email", user.Email));
                        await userManager.AddClaimAsync(newUser, new System.Security.Claims.Claim("Role", user.Role));
                        await userManager.AddClaimAsync(newUser, new System.Security.Claims.Claim("Department", user.Department));
                    }
                }
            }
        }
    }
}
