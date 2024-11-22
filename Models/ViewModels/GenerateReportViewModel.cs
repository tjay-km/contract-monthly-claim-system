using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CMCS.Models
{
    public class GenerateReportViewModel
    {
        [Required]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Please select a report type.")]
        [Display(Name = "Report Type")]
        [RegularExpression("^(Summary|Detailed|Department)$", ErrorMessage = "Invalid report type selected.")]
        public string ReportType { get; set; }

        public List<SelectListItem> ReportTypes { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Summary", Text = "Summary Report" },
            new SelectListItem { Value = "Detailed", Text = "Detailed Report" },
            new SelectListItem { Value = "Department", Text = "Department Report" }
        };
    }
}
