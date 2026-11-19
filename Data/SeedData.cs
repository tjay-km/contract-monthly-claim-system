using System.Linq;
using System.Threading.Tasks;
using CMCS.Models;
using Microsoft.AspNetCore.Identity;
using SecurityClaim = System.Security.Claims.Claim;

namespace CMCS.Data
{
    public static class SeedData
    {
        public static readonly string[] Roles = { "Lecturer", "ProgrammeCoordinator", "AcademicManager" };

        private static readonly (string Role, string[] Permissions)[] RolePermissions =
        {
            ("Lecturer", new[] { "SubmitClaim", "ViewOwnClaims" }),
            ("ProgrammeCoordinator", new[] { "ReviewClaims", "ApproveClaims", "RejectClaims" }),
            ("AcademicManager", new[] { "ReviewClaims", "ApproveClaims", "RejectClaims", "ViewAllClaims" })
        };

        /// <summary>Creates the roles and their permission claims. Safe to run on every start.</summary>
        public static async Task InitializeRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            foreach (var (roleName, permissions) in RolePermissions)
            {
                var role = await roleManager.FindByNameAsync(roleName);
                if (role == null)
                {
                    role = new IdentityRole(roleName);
                    await roleManager.CreateAsync(role);
                }

                var existing = (await roleManager.GetClaimsAsync(role))
                    .Where(c => c.Type == "Permission")
                    .Select(c => c.Value)
                    .ToHashSet();

                foreach (var permission in permissions.Where(p => !existing.Contains(p)))
                    await roleManager.AddClaimAsync(role, new SecurityClaim("Permission", permission));
            }
        }

        /// <summary>
        /// Creates one demo account per role. Only called in the Development environment.
        /// </summary>
        public static async Task SeedDemoUsersAsync(UserManager<ApplicationUser> userManager, string password)
        {
            var demoUsers = new[]
            {
                (Email: "lecturer@test.com", Role: "Lecturer", First: "Demo", Last: "Lecturer", Department: "Computer Science", Type: UserType.Lecturer),
                (Email: "coordinator@test.com", Role: "ProgrammeCoordinator", First: "Demo", Last: "Coordinator", Department: "Information Technology", Type: UserType.HR),
                (Email: "manager@test.com", Role: "AcademicManager", First: "Demo", Last: "Manager", Department: "Academic Affairs", Type: UserType.HR)
            };

            foreach (var demo in demoUsers)
            {
                if (await userManager.FindByEmailAsync(demo.Email) != null)
                    continue;

                var user = new ApplicationUser
                {
                    UserName = demo.Email,
                    Email = demo.Email,
                    EmailConfirmed = true,
                    FirstName = demo.First,
                    LastName = demo.Last,
                    Name = $"{demo.First} {demo.Last}",
                    Department = demo.Department,
                    Role = demo.Role,
                    UserType = demo.Type
                };

                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(user, demo.Role);
            }
        }
    }
}
