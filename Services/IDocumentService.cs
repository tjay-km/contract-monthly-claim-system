using CMCS.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace CMCS.Services
{
    public interface IDocumentService
    {
        Task<string> SaveDocumentAsync(IFormFile file);
        Task<string> AddClaimDocument(ClaimDocument claimDocument, IFormFile file);
        Task<List<ClaimDocument>> GetClaimDocuments(int claimId);
        Task DeleteClaimDocument(int claimDocumentId);
    }
}
