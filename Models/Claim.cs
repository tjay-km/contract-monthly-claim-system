using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace CMCS.Models
{
    public class Claim
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Lecturer name is required")]
        [Display(Name = "Lecturer Name")]
        public string LecturerName { get; set; }

        [Required(ErrorMessage = "Hours worked is required")]
        [Display(Name = "Hours Worked")]
        [Range(0.5, 195, ErrorMessage = "Hours worked must be between 0.5 and 195")]
        public decimal HoursWorked { get; set; }

        [Required(ErrorMessage = "Hourly rate is required")]
        [Display(Name = "Hourly Rate")]
        [Range(1, 20000, ErrorMessage = "Hourly rate must be between 1 and 20000")]
        [DataType(DataType.Currency)]
        public decimal HourlyRate { get; set; }

        [Display(Name = "Total Amount")]
        [DataType(DataType.Currency)]
        public decimal TotalAmount => Math.Round(HoursWorked * HourlyRate, 2);

        [Required(ErrorMessage = "Date submitted is required")]
        [Display(Name = "Date Submitted")]
        [DataType(DataType.Date)]
        public DateTime DateSubmitted { get; set; }

        [Required(ErrorMessage = "Additional notes are required")]
        [Display(Name = "Additional Notes")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Additional notes must be between 10 and 500 characters")]
        public string AdditionalNotes { get; set; }

        [Required(ErrorMessage = "Status is required")]
        public string Status { get; set; } = "Pending";

        [Display(Name = "Rejection Reason")]
        public string? RejectionReason { get; set; }

        [Required(ErrorMessage = "Supporting document is required")]
        [Display(Name = "Supporting Document")]
        public IFormFile SupportingDocument { get; set; }

        public string? SupportingDocumentPath { get; set; }

        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
    }
}
