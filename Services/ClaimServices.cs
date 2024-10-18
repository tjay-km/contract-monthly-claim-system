using CMCS.Models;
using System.Collections.Generic;
using System.Linq;

namespace CMCS.Services
{
    public class ClaimsService : IClaimsService
    {
        private readonly List<Claim> _claims; // In-memory list to store claims

        public ClaimsService(List<Claim> claims)
        {
            _claims = claims; // Initialize the claims list
        }

        // Method to get all claims
        public List<Claim> GetClaims()
        {
            return _claims; // Return the entire list of claims
        }

        // Method to get a specific claim by ID
        public Claim GetClaimById(int id)
        {
            return _claims.FirstOrDefault(c => c.Id == id); // Return the claim if found, otherwise null
        }

        // Method to update the status of a claim
        public void UpdateClaimStatus(int id, string status)
        {
            var claim = GetClaimById(id); // Get the claim by ID
            if (claim != null) // If the claim exists
            {
                claim.Status = status; // Update the claim's status
            }
        }
    }
}
