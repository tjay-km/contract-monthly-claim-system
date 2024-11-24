using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using CMCS.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace CMCS.Data
{
    public static class SeedData
    {
        public static async Task InitializeRolesAndClaims(
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager)
        {
            try
            {
                // Create roles if they don't exist
                string[] roles = { "Lecturer", "ProgrammeCoordinator", "AcademicManager" };
                foreach (var roleName in roles)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        var identityRole = new IdentityRole(roleName)
                        {
                            NormalizedName = roleName.ToUpper()
                        };
                        var result = await roleManager.CreateAsync(identityRole);
                        if (!result.Succeeded)
                        {
                            throw new Exception($"Failed to create role: {roleName}");
                        }
                    }

                    var role = await roleManager.FindByNameAsync(roleName);
                    if (role == null) continue;

                    var claims = await roleManager.GetClaimsAsync(role);
                    if (!claims.Any(c => c.Type == "Permission"))
                    {
                        switch (roleName)
                        {
                            case "Lecturer":
                                await roleManager.AddClaimAsync(role, new System.Security.Claims.Claim("Permission", "SubmitClaim"));
                                await roleManager.AddClaimAsync(role, new System.Security.Claims.Claim("Permission", "ViewOwnClaims"));
                                break;

                            case "ProgrammeCoordinator":
                                await roleManager.AddClaimAsync(role, new System.Security.Claims.Claim("Permission", "ReviewClaims"));
                                await roleManager.AddClaimAsync(role, new System.Security.Claims.Claim("Permission", "ApproveClaims"));
                                await roleManager.AddClaimAsync(role, new System.Security.Claims.Claim("Permission", "RejectClaims"));
                                await roleManager.AddClaimAsync(role, new System.Security.Claims.Claim("MaxApprovalAmount", "5000"));
                                break;

                            case "AcademicManager":
                                await roleManager.AddClaimAsync(role, new System.Security.Claims.Claim("Permission", "ReviewClaims"));
                                await roleManager.AddClaimAsync(role, new System.Security.Claims.Claim("Permission", "ApproveClaims"));
                                await roleManager.AddClaimAsync(role, new System.Security.Claims.Claim("Permission", "RejectClaims"));
                                await roleManager.AddClaimAsync(role, new System.Security.Claims.Claim("Permission", "ViewAllClaims"));
                                await roleManager.AddClaimAsync(role, new System.Security.Claims.Claim("MaxApprovalAmount", "20000"));
                                break;
                        }
                    }
                }

                // Add default test users if they don't exist
                var testUsers = new[]
                {
                    new 
                    { 
                        Email = "lecturer@test.com",
                        UserName = "lecturer@test.com",
                        Role = "Lecturer",
                        Department = "Computer Science",
                        FirstName = "Lecturer",
                        LastName = "User"
                    },
                    new 
                    { 
                        Email = "coordinator@test.com",
                        UserName = "coordinator@test.com",
                        Role = "ProgrammeCoordinator",
                        Department = "Information Technology",
                        FirstName = "Coordinator",
                        LastName = "User"
                    },
                    new 
                    { 
                        Email = "manager@test.com",
                        UserName = "manager@test.com",
                        Role = "AcademicManager",
                        Department = "Academic Affairs",
                        FirstName = "Manager",
                        LastName = "User"
                    }
                };

                foreach (var userData in testUsers)
                {
                    var existingUser = await userManager.FindByEmailAsync(userData.Email);
                    if (existingUser == null)
                    {
                        var newUser = new ApplicationUser
                        {
                            UserName = userData.UserName,
                            NormalizedUserName = userData.UserName.ToUpper(),
                            Email = userData.Email,
                            NormalizedEmail = userData.Email.ToUpper(),
                            EmailConfirmed = true,
                            Department = userData.Department,
                            FirstName = userData.FirstName,
                            LastName = userData.LastName,
                            Name = $"{userData.FirstName} {userData.LastName}",
                            Role = userData.Role,
                            UserType = GetUserType(userData.Role),
                            IsActive = true,
                            SecurityStamp = Guid.NewGuid().ToString("D"),
                            ConcurrencyStamp = Guid.NewGuid().ToString("D"),
                            LockoutEnabled = true,
                            PhoneNumberConfirmed = false
                        };

                        var result = await userManager.CreateAsync(newUser, "Test@123");
                        if (result.Succeeded)
                        {
                            await userManager.AddToRoleAsync(newUser, userData.Role);

                            // Add user-specific claims
                            await userManager.AddClaimAsync(newUser, new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, userData.Email));
                            await userManager.AddClaimAsync(newUser, new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, userData.Role));
                            await userManager.AddClaimAsync(newUser, new System.Security.Claims.Claim("Department", userData.Department));
                        }
                        else
                        {
                            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                            throw new Exception($"Failed to create user {userData.Email}: {errors}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Seed data initialization failed: {ex.Message}", ex);
            }
        }

        private static UserType GetUserType(string role)
        {
            return role?.ToUpper() switch
            {
                "LECTURER" => UserType.Lecturer,
                "PROGRAMMECOORDINATOR" => UserType.HOD,
                "ACADEMICMANAGER" => UserType.HR,
                _ => UserType.Lecturer
            };
        }

        public static async Task InitializeDepartmentsAndModules(ApplicationDbContext context)
        {
            // Check if departments already exist
            if (!await context.Departments.AnyAsync())
            {
                var departments = new[]
                {
                    new Department { Name = "Information Technology", Code = "IT", IsActive = true },
                    new Department { Name = "Computer Science", Code = "CS", IsActive = true },
                    new Department { Name = "Software Engineering", Code = "SE", IsActive = true },
                    new Department { Name = "Information Systems", Code = "IS", IsActive = true },
                    new Department { Name = "Artificial Intelligence", Code = "AI", IsActive = true }
                };

                await context.Departments.AddRangeAsync(departments);
                await context.SaveChangesAsync();

                // Add modules for each department
                var modules = new List<Module>();
                foreach (var dept in departments)
                {
                    modules.AddRange(new[]
                    {
                        new Module { 
                            Code = $"{dept.Code}101", 
                            Name = $"Introduction to {dept.Name}", 
                            Credits = 15, 
                            DepartmentId = dept.Id,
                            IsActive = true 
                        },
                        new Module { 
                            Code = $"{dept.Code}201", 
                            Name = $"Intermediate {dept.Name}", 
                            Credits = 15, 
                            DepartmentId = dept.Id,
                            IsActive = true 
                        },
                        new Module { 
                            Code = $"{dept.Code}301", 
                            Name = $"Advanced {dept.Name}", 
                            Credits = 15, 
                            DepartmentId = dept.Id,
                            IsActive = true 
                        }
                    });
                }

                await context.Modules.AddRangeAsync(modules);
                await context.SaveChangesAsync();
            }
        }
    }
}
