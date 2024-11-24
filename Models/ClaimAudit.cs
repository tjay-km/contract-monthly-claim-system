using System;

namespace CMCS.Models
{
    public class ClaimAudit
    {
        public ClaimAudit()
        {
            Action = string.Empty;
            PerformedBy = string.Empty;
            Details = string.Empty;
            Timestamp = DateTime.Now;
        }

        public int Id { get; set; }
        public string Action { get; set; }
        public string PerformedBy { get; set; }
        public DateTime Timestamp { get; set; }
        public string Details { get; set; }
        public int? ClaimId { get; set; }
        public virtual Claim? Claim { get; set; }
    }
}
