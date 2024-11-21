using CMCS.Models;
using CMCS.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace CMCS.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly ApplicationDbContext _context;

        public DocumentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> AddClaimDocument(ClaimDocument claimDocument)
        {
            _context.ClaimDocuments.Add(claimDocument);
            await _context.SaveChangesAsync();
            return claimDocument.Id;
        }

        public async Task<List<ClaimDocument>> GetClaimDocuments(int claimId)
        {
            return await _context.ClaimDocuments
                .Where(x => x.ClaimId == claimId)
                .ToListAsync();
        }

        public async Task DeleteClaimDocument(int claimDocumentId)
        {
            var claimDocument = await _context.ClaimDocuments.FindAsync(claimDocumentId);
            if (claimDocument != null)
            {
                _context.ClaimDocuments.Remove(claimDocument);
                await _context.SaveChangesAsync();
            }
        }
    }
}
