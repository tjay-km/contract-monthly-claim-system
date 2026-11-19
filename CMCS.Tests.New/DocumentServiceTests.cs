using CMCS.Models;
using CMCS.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace CMCS.Tests
{
    [TestFixture]
    public class DocumentServiceTests
    {
        private string _contentRoot = null!;
        private DocumentService _service = null!;

        [SetUp]
        public void SetUp()
        {
            _contentRoot = Path.Combine(Path.GetTempPath(), "cmcs-tests-" + Guid.NewGuid().ToString("N"));
            var environment = Mock.Of<IWebHostEnvironment>(e => e.ContentRootPath == _contentRoot);
            var settings = Options.Create(new FileUploadSettings { MaxFileSize = 1024, AllowedFileTypes = new[] { ".pdf", ".docx" } });
            _service = new DocumentService(environment, settings);
        }

        [TearDown]
        public void TearDown() => Directory.Delete(_contentRoot, recursive: true);

        [Test]
        public void Validate_AcceptsAllowedFileRegardlessOfCase() =>
            Assert.That(_service.Validate(TestHelpers.File("timesheet.PDF", 100)), Is.Null);

        [Test]
        public void Validate_RejectsMissingOrEmptyFile()
        {
            Assert.That(_service.Validate(null), Is.Not.Null);
            Assert.That(_service.Validate(TestHelpers.File("empty.pdf", 0)), Is.Not.Null);
        }

        [Test]
        public void Validate_RejectsDisallowedType() =>
            Assert.That(_service.Validate(TestHelpers.File("script.exe", 100)), Does.Contain("Only"));

        [Test]
        public void Validate_RejectsTooLargeFile() =>
            Assert.That(_service.Validate(TestHelpers.File("big.pdf", 2048)), Does.Contain("too large"));

        [Test]
        public async Task Save_StoresOutsideWebRootUnderGeneratedName()
        {
            var stored = await _service.SaveDocumentAsync(TestHelpers.File("..\\..\\evil name.pdf", 100));

            Assert.That(stored, Does.Match("^[0-9a-f]{32}\\.pdf$"));
            var path = _service.GetDocumentPath(stored);
            Assert.That(path, Is.Not.Null);
            Assert.That(path, Does.StartWith(Path.Combine(_contentRoot, "App_Data", "uploads")));
        }

        [Test]
        public void Save_RefusesInvalidFile() =>
            Assert.ThrowsAsync<InvalidOperationException>(() => _service.SaveDocumentAsync(TestHelpers.File("x.exe", 10)));

        [Test]
        public async Task GetDocumentPath_IgnoresDirectoryTraversal()
        {
            var stored = await _service.SaveDocumentAsync(TestHelpers.File("a.pdf", 10));

            Assert.That(_service.GetDocumentPath("../../" + stored), Is.EqualTo(_service.GetDocumentPath(stored)));
            Assert.That(_service.GetDocumentPath("../appsettings.json"), Is.Null);
            Assert.That(_service.GetDocumentPath(null), Is.Null);
        }
    }
}
