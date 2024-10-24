using CMCS.Models;
public interface IClaimService
{
    List<Claim> GetClaims();
    Claim GetClaimById(int id);
    bool UpdateStatus(int id, string status);
    List<Claim> GetPendingClaims();
    List<Claim> GetManagedClaims();
}
