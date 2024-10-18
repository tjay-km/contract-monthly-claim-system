using System.ComponentModel.DataAnnotations;

public class Claim
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Lecturer name is required.")]
    public string LecturerName { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Hours worked must be a positive number.")]
    public double HoursWorked { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Hourly rate must be a positive number.")]
    public decimal HourlyRate { get; set; }

    public string Status { get; set; }

    public string SupportingDocumentPath { get; set; }

    [StringLength(500, ErrorMessage = "Additional notes cannot exceed 500 characters.")]
    public string AdditionalNotes { get; set; }

    public DateTime DateSubmitted { get; internal set; }

    public Claim()
    {
        DateSubmitted = DateTime.Now; // Set the submission date to now when creating a new claim
    }


}
