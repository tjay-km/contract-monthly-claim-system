using CMCS.Models;
using System.Collections.Generic;
using System.Linq;

namespace CMCS.Services
{
    public class ClaimsService : IClaimsService
    {
        private readonly List<Claim> _claims;

        public ClaimsService(List<Claim> claims)
        {
            _claims = claims;
        }

        public List<Claim> GetClaims()
        {
            return _claims;
        }

        public Claim GetClaimById(int id)
        {
            return _claims.FirstOrDefault(c => c.Id == id);
        }

        public void UpdateClaimStatus(int id, string status)
        {
            var claim = GetClaimById(id);
            if (claim != null)
            {
                claim.Status = status;
            }
        }
    }
}
