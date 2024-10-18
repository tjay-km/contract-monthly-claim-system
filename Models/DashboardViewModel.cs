namespace CMCS.Models
{
    public class DashboardViewModel
    {
        public Claim Claim { get; set; } = new Claim(); // For submitting a new claim
        public List<Claim> Claims { get; set; } = new List<Claim>(); // List of submitted claims
        public List<Claim> PendingClaims { get; set; } = new List<Claim>(); // List of pending claims


    }
}
