using System;

namespace CMCS.Models
{
    public class ClaimDocument
    {
        public int Id { get; set; }              // Unique identifier for each document
        public int ClaimId { get; set; }         // The ID of the claim to which this document is linked
        public string FileName { get; set; }     // The name of the uploaded file
        public string FilePath { get; set; }     // The path where the file is stored
        public DateTime DateUploaded { get; set; }  // The date the file was uploaded
    }
}
