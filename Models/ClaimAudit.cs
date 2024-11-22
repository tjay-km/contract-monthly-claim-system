using System;

namespace CMCS.Models
{
    public class ClaimAudit
    {
        public int Id { get; set; }
        public string Action { get; set; }
        public string PerformedBy { get; set; }
        public DateTime Timestamp { get; set; }
        public string Details { get; set; }
        public int? ClaimId { get; set; }
        public virtual Claim? Claim { get; set; }
    }
}
