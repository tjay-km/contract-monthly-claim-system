using CMCS.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMCS.Services
{
    public interface IDocumentService
    {
        Task<int> AddClaimDocument(ClaimDocument claimDocument);
        Task<List<ClaimDocument>> GetClaimDocuments(int claimId);
        Task DeleteClaimDocument(int claimDocumentId);
    }
}
