using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CMCS.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CMCS.Services
{
    /// <summary>
    /// Stores supporting documents in App_Data/uploads, which is not served as static content,
    /// so files can only be downloaded through the authorised DownloadDocument action.
    /// </summary>
    public class DocumentService : IDocumentService
    {
        private readonly FileUploadSettings _settings;
        private readonly string _uploadDirectory;

        public DocumentService(IWebHostEnvironment environment, IOptions<FileUploadSettings> settings)
        {
            _settings = settings.Value;
            _uploadDirectory = Path.Combine(environment.ContentRootPath, "App_Data", "uploads");
            Directory.CreateDirectory(_uploadDirectory);
        }

        public string? Validate(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return "Please attach a supporting document.";

            if (file.Length > _settings.MaxFileSize)
                return $"The file is too large. The maximum size is {_settings.MaxFileSize / (1024 * 1024)} MB.";

            var extension = Path.GetExtension(file.FileName);
            if (!_settings.AllowedFileTypes.Contains(extension, StringComparer.OrdinalIgnoreCase))
                return $"Only {string.Join(", ", _settings.AllowedFileTypes)} files are allowed.";

            return null;
        }

        public async Task<string> SaveDocumentAsync(IFormFile file)
        {
            var error = Validate(file);
            if (error != null)
                throw new InvalidOperationException(error);

            // Never trust the client's file name: keep only its extension.
            var storedFileName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
            await using var stream = new FileStream(Path.Combine(_uploadDirectory, storedFileName), FileMode.CreateNew);
            await file.CopyToAsync(stream);
            return storedFileName;
        }

        public string? GetDocumentPath(string? storedFileName)
        {
            if (string.IsNullOrWhiteSpace(storedFileName))
                return null;

            // GetFileName strips any directory parts, which blocks path traversal.
            var path = Path.Combine(_uploadDirectory, Path.GetFileName(storedFileName));
            return File.Exists(path) ? path : null;
        }
    }
}
