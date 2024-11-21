using System.ComponentModel.DataAnnotations;

namespace CMCS.Models
{
    public class ClaimRejectionModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please provide a reason for rejection")]
        [Display(Name = "Reason for Rejection")]
        public string RejectionReason { get; set; }
    }
}
