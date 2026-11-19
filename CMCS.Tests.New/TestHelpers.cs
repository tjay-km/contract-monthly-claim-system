using System.Security.Claims;
using CMCS.Data;
using CMCS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CMCS.Tests
{
    internal static class TestHelpers
    {
        public static Mock<UserManager<ApplicationUser>> CreateUserManager() =>
            new(Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);

        public static ApplicationUser User(string id, string name = "Test User") =>
            new() { Id = id, UserName = $"{id}@test.com", Email = $"{id}@test.com", Name = name, Department = "IT" };

        /// <summary>Gives a controller a signed-in user with the given roles.</summary>
        public static void SignIn(Controller controller, string userId, params string[] roles)
        {
            var claims = new List<System.Security.Claims.Claim>
            {
                new(ClaimTypes.NameIdentifier, userId),
                new(ClaimTypes.Name, userId)
            };
            claims.AddRange(roles.Select(role => new System.Security.Claims.Claim(ClaimTypes.Role, role)));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) }
            };
            controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());
        }

        public static IFormFile File(string fileName, int sizeInBytes) =>
            new FormFile(new MemoryStream(new byte[sizeInBytes]), 0, sizeInBytes, "supportingDocument", fileName);

        /// <summary>A real ApplicationDbContext backed by an in-memory SQLite database.</summary>
        public static (ApplicationDbContext Context, SqliteConnection Connection) CreateDatabase()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();
            return (context, connection);
        }
    }
}
