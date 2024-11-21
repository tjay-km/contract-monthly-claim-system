using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace CMCS.Models
{
    public class Claim
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Lecturer Name")]
        public string LecturerName { get; set; }

        [Display(Name = "Date Submitted")]
        public DateTime DateSubmitted { get; set; }

        [Required]
        [Display(Name = "Hours Worked")]
        [Range(0.1, 10000, ErrorMessage = "Hours worked must be between 0.1 and 10000")]
        public decimal HoursWorked { get; set; }

        [Required]
        [Display(Name = "Hourly Rate")]
        [Range(0.1, 10000, ErrorMessage = "Hourly rate must be between 0.1 and 10000")]
        public decimal HourlyRate { get; set; }

        [Display(Name = "Additional Notes")]
        public string AdditionalNotes { get; set; }

        public string Status { get; set; } = "Pending";

        [Display(Name = "Rejection Reason")]
        public string? RejectionReason { get; set; }

        [Display(Name = "Supporting Document")]
        public IFormFile SupportingDocument { get; set; }

        public string? SupportingDocumentPath { get; set; }

        public decimal CalculateTotalAmount()
        {
            return HoursWorked * HourlyRate;
        }
    }
}
