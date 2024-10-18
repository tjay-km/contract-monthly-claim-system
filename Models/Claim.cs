using Microsoft.AspNetCore.Http;
using System;

namespace CMCS.Models
{
    public class Claim
    {
        public int Id { get; set; }
        public string LecturerName { get; set; }
        public DateTime DateSubmitted { get; set; }
        public decimal HoursWorked { get; set; }
        public decimal HourlyRate { get; set; }
        public string AdditionalNotes { get; set; }
        public string Status { get; set; } = "Pending";

        public IFormFile SupportingDocument { get; set; }
        public string? SupportingDocumentPath { get; set; } // To store the file path
    }
}
