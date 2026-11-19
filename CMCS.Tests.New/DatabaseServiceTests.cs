using CMCS.Data;
using CMCS.Models;
using CMCS.Models.Enums;
using CMCS.Services;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using ClaimModel = CMCS.Models.Claim;

namespace CMCS.Tests
{
    /// <summary>Services that talk to the database, run against a real (in-memory SQLite) database.</summary>
    [TestFixture]
    public class DatabaseServiceTests
    {
        private ApplicationDbContext _db = null!;
        private SqliteConnection _connection = null!;
        private const string LecturerId = "lecturer-1";

        [SetUp]
        public void SetUp()
        {
            (_db, _connection) = TestHelpers.CreateDatabase();
            _db.Users.Add(new ApplicationUser { Id = LecturerId, UserName = "l1", Name = "Lecturer One", Role = "Lecturer" });
            _db.SaveChanges();
        }

        [TearDown]
        public void TearDown()
        {
            _db.Dispose();
            _connection.Dispose();
        }

        private void GivenExistingClaim(DateTime submitted)
        {
            _db.Claims.Add(new ClaimModel
            {
                LecturerId = LecturerId,
                LecturerName = "Lecturer One",
                LecturerDepartment = "IT",
                ModuleCode = "PROG6212",
                AdditionalNotes = "Earlier claim",
                HoursWorked = 1,
                HourlyRate = 100,
                TotalAmount = 100,
                SubmissionDate = submitted,
                Status = ClaimStatus.Approved
            });
            _db.SaveChanges();
        }

        private static ClaimModel NewClaim(decimal hours = 10, decimal rate = 250) => new()
        {
            LecturerId = LecturerId,
            ModuleCode = "PROG6212",
            HoursWorked = hours,
            HourlyRate = rate,
            TotalAmount = 1 // deliberately wrong; the service recalculates it
        };

        [Test]
        public async Task Validation_AcceptsAValidClaimAndRecalculatesTheTotal()
        {
            var claim = NewClaim();

            var (isValid, messages) = await new ClaimValidationService(_db).ValidateClaimAsync(claim);

            Assert.That(isValid, Is.True, string.Join("; ", messages));
            Assert.That(claim.TotalAmount, Is.EqualTo(2500m));
        }

        [Test]
        public async Task Validation_RejectsOutOfRangeHoursAndRate()
        {
            var (isValid, messages) = await new ClaimValidationService(_db).ValidateClaimAsync(NewClaim(hours: 200, rate: 0));

            Assert.That(isValid, Is.False);
            Assert.That(messages, Has.Some.Contains("Hours worked"));
            Assert.That(messages, Has.Some.Contains("Hourly rate"));
        }

        [Test]
        public async Task Validation_RequiresTwentyFourHoursBetweenClaims()
        {
            GivenExistingClaim(DateTime.Now.AddHours(-2));

            var (isValid, messages) = await new ClaimValidationService(_db).ValidateClaimAsync(NewClaim());

            Assert.That(isValid, Is.False);
            Assert.That(messages, Has.Some.Contains("24 hours"));
        }

        [Test]
        public async Task Validation_LimitsClaimsPerMonth()
        {
            var monthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            for (var i = 0; i < ClaimValidationService.MaxClaimsPerMonth; i++)
                GivenExistingClaim(monthStart.AddMinutes(i));

            var (_, messages) = await new ClaimValidationService(_db).ValidateClaimAsync(NewClaim());

            Assert.That(messages, Has.Some.Contains("per month"));
        }

        [Test]
        public async Task Validation_RejectsUnknownLecturer()
        {
            var claim = NewClaim();
            claim.LecturerId = "nobody";

            var (isValid, _) = await new ClaimValidationService(_db).ValidateClaimAsync(claim);

            Assert.That(isValid, Is.False);
        }

        [Test]
        public async Task AutoApprovalConfig_DefaultsWhenNothingSaved()
        {
            var config = await new AutoApprovalConfigService(_db).GetCurrentConfigAsync();

            Assert.That(config.EnableAutoApproval, Is.True);
            Assert.That(config.MaxAutoApprovalAmount, Is.EqualTo(5000m));
        }

        [Test]
        public async Task AutoApprovalConfig_UsesTheLatestSavedVersion()
        {
            var service = new AutoApprovalConfigService(_db);
            await service.UpdateConfigAsync(new AutoApprovalConfig { EnableAutoApproval = true, MaxAutoApprovalAmount = 1000m }, "hr");
            await service.UpdateConfigAsync(new AutoApprovalConfig { EnableAutoApproval = false, MaxAutoApprovalAmount = 2000m }, "hr");

            Assert.That(await service.IsEligibleForAutoApprovalAsync(500m), Is.False, "auto-approval was switched off");
            Assert.That((await service.GetCurrentConfigAsync()).MaxAutoApprovalAmount, Is.EqualTo(2000m));
            Assert.That(_db.AutoApprovalConfigs.Count(), Is.EqualTo(2), "older versions are kept as history");
        }
    }
}
