using System;
using System.ComponentModel.DataAnnotations;

namespace CMCS.Models.ViewModels
{
    public class UserSummaryViewModel
    {
        public UserSummaryViewModel()
        {
            FirstName = string.Empty;
            LastName = string.Empty;
            Email = string.Empty;
            Department = string.Empty;
            Role = string.Empty;
            Name = string.Empty;
            UserId = string.Empty;
        }

        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Department { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public int TotalClaims { get; set; }
        public int PendingClaims { get; set; }
        public decimal TotalClaimAmount { get; set; }
        public DateTime? LastClaimDate { get; set; }
    }
}
