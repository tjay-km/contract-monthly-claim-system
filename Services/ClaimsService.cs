using CMCS.Models;
using CMCS.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace CMCS.Services
{
    public class ClaimsService : IClaimsService
    {
        private readonly ApplicationDbContext _context;

        public ClaimsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Claim>> GetAllClaimsAsync()
        {
            return await _context.Claims.ToListAsync();
        }

        public async Task<Claim> GetClaimByIdAsync(int id)
        {
            return await _context.Claims.FindAsync(id);
        }

        public async Task<Claim> CreateClaimAsync(Claim claim)
        {
            _context.Claims.Add(claim);
            await _context.SaveChangesAsync();
            return claim;
        }

        public async Task<Claim> UpdateClaimAsync(Claim claim)
        {
            _context.Entry(claim).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return claim;
        }

        public async Task<bool> DeleteClaimAsync(int id)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null)
            {
                return false;
            }

            _context.Claims.Remove(claim);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
