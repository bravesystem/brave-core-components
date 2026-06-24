using Microsoft.AspNetCore.Http.HttpResults;
using System;

namespace BRaVe_Management_Backend.Models
{
    public class Surveys
    {
        public int SurveyId { get; set; }
        public int ProgramId { get; set; }
        public string ProgramName { get; set; }
        public string SurveyCode { get; set; }
        public int SurveyType { get; set; }
        public int? TenantId { get; set; }
        public string? Title { get; set; }
        public string  Details { get; set; } 
        public bool? IsActive { get; set; } 
        public string CreatedByUserId { get; set; }
        public string UpdatedByUserId { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
        public int TotalQuestions { get; set; } = 0;
        
    }
}
