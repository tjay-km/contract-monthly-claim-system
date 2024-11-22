using CMCS.Models;

namespace CMCS.Models.ViewModels
{
    public class DashboardViewModel
    {
        public Claim Claim { get; set; } = new Claim();
        public List<Claim> Claims { get; set; } = new List<Claim>();
        public List<Claim> PendingClaims { get; set; } = new List<Claim>();
        public List<Claim> ManagedClaims { get; set; } = new List<Claim>();
    }
}
