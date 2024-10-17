namespace CMCS.Models
{
    public class Claim
    {
        public int Id { get; set; }
        public string LecturerName { get; set; }
        public double HoursWorked { get; set; }
        public decimal HourlyRate { get; set; }
        public string Status { get; set; }
        public string SupportingDocumentPath { get; set; }
        public string AdditionalNotes { get; set; }
    }
}