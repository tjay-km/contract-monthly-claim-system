using CMCS.Models;
using System.Collections.Generic;

namespace CMCS.Services
{
    public interface IClaimsService
    {
        List<Claim> GetClaims(); // Get all claims
        Claim GetClaimById(int id); // Get a specific claim by ID
        void UpdateClaimStatus(int id, string status); // Update claim status
    }
}
