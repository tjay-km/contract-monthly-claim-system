using System;
using System.ComponentModel.DataAnnotations;

namespace CMCS.Models
{
    public class ClaimDocument
    {
        public int Id { get; set; }              // Unique identifier for each document
        
        [Required]
        public int ClaimId { get; set; }         // The ID of the claim to which this document is linked
        
        [Required]
        [MaxLength(255)]
        public string FileName { get; set; }     // The name of the uploaded file
        
        [Required]
        [MaxLength(1000)]
        public string FilePath { get; set; }     // The path where the file is stored
        
        public DateTime DateUploaded { get; set; }  // The date the file was uploaded

        public virtual Claim Claim { get; set; }    // Navigation property to the associated claim
    }
}
