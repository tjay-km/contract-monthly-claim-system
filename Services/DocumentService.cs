using CMCS.Models; // Replace with your actual namespace

namespace CMCS.Services
{
    public class DocumentService
    {
        private List<ClaimDocument> claimDocuments = new List<ClaimDocument>(); // In-memory list

        // Add a new document
        public int AddClaimDocument(ClaimDocument claimDocument)
        {
            claimDocument.Id = claimDocuments.Count + 1; // Generate Id manually
            claimDocuments.Add(claimDocument);
            return claimDocument.Id;
        }

        // Delete a document
        public void DeleteClaimDocument(int claimDocumentId)
        {
            var claimDocument = claimDocuments.FirstOrDefault(x => x.Id == claimDocumentId);
            if (claimDocument != null)
            {
                claimDocuments.Remove(claimDocument);
            }
        }

        // Get documents for a claim
        public List<ClaimDocument> GetClaimDocuments(int claimId)
        {
            return claimDocuments.Where(x => x.ClaimId == claimId).ToList();
        }

}
}
