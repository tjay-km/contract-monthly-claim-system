using System;
using CMCS.Models.Enums;

namespace CMCS.Models.ViewModels
{
    public class ClaimSummaryViewModel
    {
        public ClaimSummaryViewModel()
        {
            LecturerName = string.Empty;
            Department = string.Empty;
            Description = string.Empty;
        }

        public int ClaimId { get; set; }
        public string LecturerName { get; set; }
        public string Department { get; set; }
        public string Description { get; set; }
        public decimal TotalAmount { get; set; }
        public ClaimStatus Status { get; set; }
        public DateTime SubmissionDate { get; set; }
    }
}
