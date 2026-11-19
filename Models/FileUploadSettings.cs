using System;

namespace CMCS.Models
{
    /// <summary>
    /// Upload limits, bound from the "FileUploadSettings" section of appsettings.json.
    /// </summary>
    public class FileUploadSettings
    {
        public const string SectionName = "FileUploadSettings";

        /// <summary>Maximum upload size in bytes.</summary>
        public long MaxFileSize { get; set; } = 5 * 1024 * 1024;

        /// <summary>Allowed file extensions, including the leading dot.</summary>
        public string[] AllowedFileTypes { get; set; } = { ".pdf", ".docx", ".xlsx" };
    }
}
