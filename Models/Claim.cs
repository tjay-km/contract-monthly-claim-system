using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using CMCS.Models.Enums;

namespace CMCS.Models
{
    public class Claim
    {
        public Claim()
        {
            LecturerId = string.Empty;
            LecturerName = string.Empty;
            LecturerDepartment = string.Empty;
            AdditionalNotes = string.Empty;
            ModuleCode = string.Empty;
            Status = ClaimStatus.Pending;
            SubmissionDate = DateTime.Now;
            Documents = new List<ClaimDocument>();
        }

        public int Id { get; set; }

        [Required]
        public string LecturerId { get; set; }

        [Required(ErrorMessage = "Lecturer name is required")]
        [Display(Name = "Lecturer Name")]
        public string LecturerName { get; set; }

        [Required(ErrorMessage = "Department is required")]
        [Display(Name = "Department")]
        public string LecturerDepartment { get; set; }

        [Required(ErrorMessage = "Hours worked is required")]
        [Display(Name = "Hours Worked")]
        [Range(0.5, 195, ErrorMessage = "Hours worked must be between 0.5 and 195")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal HoursWorked { get; set; }

        [Required(ErrorMessage = "Hourly rate is required")]
        [Display(Name = "Hourly Rate")]
        [Range(0.01, 20000, ErrorMessage = "Hourly rate must be between 0.01 and 20000")]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyRate { get; set; }

        [Display(Name = "Total Amount")]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 3900000, ErrorMessage = "Please enter a valid amount between R0.01 and R3,900,000")]
        [Required(ErrorMessage = "Total amount is required")]
        public decimal TotalAmount { get; set; }

        [Required(ErrorMessage = "Submission date is required")]
        [Display(Name = "Submission Date")]
        [DataType(DataType.Date)]
        public DateTime SubmissionDate { get; set; }

        [Display(Name = "Date Reviewed")]
        [DataType(DataType.Date)]
        public DateTime? DateReviewed { get; set; }

        [Required(ErrorMessage = "Additional notes are required")]
        [Display(Name = "Additional Notes")]
        [MaxLength(500)]
        public string AdditionalNotes { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [Column(TypeName = "nvarchar(50)")]
        public ClaimStatus Status { get; set; }

        [Display(Name = "Rejection Reason")]
        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        [Required(ErrorMessage = "Module code is required")]
        [Display(Name = "Module Code")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Module code must be between 3 and 20 characters")]
        public string ModuleCode { get; set; }

        [NotMapped]
        [Display(Name = "Supporting Document")]
        public IFormFile? SupportingDocument { get; set; }

        [Display(Name = "Supporting Document Path")]
        [MaxLength(1000)]
        public string? SupportingDocumentPath { get; set; }

        public string? UserId { get; set; }

        public string? ReviewerId { get; set; }

        [Display(Name = "Reviewed By")]
        [StringLength(256)]
        public string? ReviewedBy { get; set; }

        [Display(Name = "Approval Comments")]
        [StringLength(500)]
        public string? ApprovalComments { get; set; }

        public virtual ApplicationUser? User { get; set; }
        public virtual ApplicationUser? Reviewer { get; set; }

        public virtual ICollection<ClaimDocument> Documents { get; set; }

        public int? ClaimReportId { get; set; }
    }
}
