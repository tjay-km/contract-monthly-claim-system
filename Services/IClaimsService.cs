using CMCS.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMCS.Services
{
    public interface IClaimsService
    {
        Task<IEnumerable<Claim>> GetAllClaimsAsync();
        Task<Claim> GetClaimByIdAsync(int id);
        Task<Claim> CreateClaimAsync(Claim claim);
        Task<Claim> UpdateClaimAsync(Claim claim);
        Task<bool> DeleteClaimAsync(int id);
    }
}
