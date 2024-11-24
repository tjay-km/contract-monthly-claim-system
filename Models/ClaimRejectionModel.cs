using System.ComponentModel.DataAnnotations;

namespace CMCS.Models
{
    public class ClaimRejectionModel
    {
        public ClaimRejectionModel()
        {
            RejectionReason = string.Empty;
        }

        public int Id { get; set; }

        [Required(ErrorMessage = "Please provide a reason for rejection")]
        [Display(Name = "Reason for Rejection")]
        [StringLength(500, ErrorMessage = "Rejection reason cannot exceed 500 characters")]
        public string RejectionReason { get; set; }
    }
}
