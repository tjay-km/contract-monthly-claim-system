using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using CMCS.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;

namespace CMCS.Tests
{
    /// <summary>
    /// Runs the real app in memory (all services, middleware and views) against SQLite instead of SQL Server.
    /// </summary>
    [TestFixture]
    public class IntegrationTests
    {
        private SqliteConnection _connection = null!;
        private WebApplicationFactory<Program> _factory = null!;
        private string _contentRoot = null!;

        [OneTimeSetUp]
        public async Task StartApp()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                    services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
                });
            });

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
            await SeedData.InitializeRolesAsync(scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>());
            _contentRoot = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>().ContentRootPath;
        }

        [OneTimeTearDown]
        public void StopApp()
        {
            _factory.Dispose();
            _connection.Dispose();
            var appData = Path.Combine(_contentRoot, "App_Data");
            if (Directory.Exists(appData))
                Directory.Delete(appData, recursive: true);
        }

        private HttpClient NewClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

        private static async Task<string> AntiForgeryTokenAsync(HttpClient client, string url)
        {
            var html = await client.GetStringAsync(url);
            var match = Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"");
            Assert.That(match.Success, Is.True, $"No anti-forgery token on {url}");
            return match.Groups[1].Value;
        }

        [TestCase("/")]
        [TestCase("/Account/Login")]
        [TestCase("/Account/Register")]
        [TestCase("/Account/AccessDenied")]
        public async Task PublicPagesRender(string url)
        {
            var response = await NewClient().GetAsync(url);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task RegisterPageNoLongerOffersARoleChoice()
        {
            var html = await NewClient().GetStringAsync("/Account/Register");

            Assert.That(html, Does.Not.Contain("AcademicManager"));
            Assert.That(html, Does.Not.Contain("name=\"Role\""));
        }

        [TestCase("/HR/Dashboard")]
        [TestCase("/Dashboard/Index")]
        [TestCase("/Claim/Submit")]
        public async Task ProtectedPagesRedirectToLogin(string url)
        {
            var response = await NewClient().GetAsync(url);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
            Assert.That(response.Headers.Location!.ToString(), Does.Contain("/Account/Login"));
        }

        [Test]
        public async Task UploadedFilesAreNotServedAsStaticContent()
        {
            var response = await NewClient().GetAsync("/uploads/anything.pdf");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task LecturerCanRegisterLogInSubmitAndSeeTheirClaim()
        {
            var client = NewClient();

            // Register (always as a lecturer)
            var register = await client.PostAsync("/Account/Register", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = await AntiForgeryTokenAsync(client, "/Account/Register"),
                ["FirstName"] = "Thandi",
                ["LastName"] = "Nkosi",
                ["Email"] = "thandi@test.com",
                ["UserName"] = "thandi",
                ["Department"] = "Information Technology",
                ["Password"] = "Str0ng!Pass",
                ["ConfirmPassword"] = "Str0ng!Pass",
                ["Role"] = "AcademicManager" // a tampered form field must be ignored
            }));
            Assert.That(register.StatusCode, Is.EqualTo(HttpStatusCode.Redirect), "registration should succeed");

            // Log in
            var login = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = await AntiForgeryTokenAsync(client, "/Account/Login"),
                ["EmailOrUsername"] = "thandi",
                ["Password"] = "Str0ng!Pass"
            }));
            Assert.That(login.StatusCode, Is.EqualTo(HttpStatusCode.Redirect), "login should succeed");

            // A self-registered user must not reach the HR area
            var hr = await client.GetAsync("/HR/Dashboard");
            Assert.That(hr.Headers.Location?.ToString(), Does.Contain("AccessDenied"));

            // Submit a claim small enough to be auto-approved
            var form = new MultipartFormDataContent
            {
                { new StringContent(await AntiForgeryTokenAsync(client, "/Claim/Submit")), "__RequestVerificationToken" },
                { new StringContent("10"), "HoursWorked" },
                { new StringContent("250"), "HourlyRate" },
                { new StringContent("Information Technology"), "LecturerDepartment" },
                { new StringContent("PROG6212"), "ModuleCode" },
                { new StringContent("Lectures and marking for the month"), "AdditionalNotes" }
            };
            var file = new ByteArrayContent(new byte[] { 37, 80, 68, 70 });
            file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            form.Add(file, "supportingDocument", "timesheet.pdf");

            var submit = await client.PostAsync("/Claim/Submit", form);
            Assert.That(submit.StatusCode, Is.EqualTo(HttpStatusCode.Redirect), await submit.Content.ReadAsStringAsync());

            // The lecturer sees their own claim, auto-approved
            var dashboard = await client.GetStringAsync("/Claim/Dashboard");
            Assert.That(dashboard, Does.Contain("Approved"));

            // ...and can download its document
            using var scope = _factory.Services.CreateScope();
            var claimId = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Claims.Single().Id;
            var download = await client.GetAsync($"/Claim/DownloadDocument/{claimId}");
            Assert.That(download.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(download.Content.Headers.ContentType!.MediaType, Is.EqualTo("application/pdf"));
        }
    }
}
