using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace CMCS.Services
{
    public interface IDocumentService
    {
        /// <summary>
        /// Returns an error message if the file breaks the upload rules (type or size), otherwise null.
        /// </summary>
        string? Validate(IFormFile? file);

        /// <summary>
        /// Saves a validated upload outside the public web root and returns the stored file name.
        /// </summary>
        Task<string> SaveDocumentAsync(IFormFile file);

        /// <summary>
        /// Returns the full path of a stored document, or null if it doesn't exist.
        /// </summary>
        string? GetDocumentPath(string? storedFileName);
    }
}
