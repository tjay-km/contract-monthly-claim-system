using CMCS.Models;
using System.Collections.Generic;

namespace CMCS.Services
{
    public interface IClaimsService
    {
        List<Claim> GetClaims();
        Claim GetClaimById(int id);
        void UpdateClaimStatus(int id, string status);
        List<Claim> GetPendingClaims();
        List<Claim> GetManagedClaims();
    }
}