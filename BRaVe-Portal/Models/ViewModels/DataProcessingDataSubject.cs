using System.Collections.Generic;

namespace BRaVe_Portal.Models.ViewModels
{
    public class DataProcessingDataSubject
    {
        public int AssessmentId { get; set; }
        public int SubjectTypeId { get; set; }
        public bool IsVulnerableGroup { get; set; } = false; 
        //public string? CreatedByUserId { get; set; }
        //public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        //public string? UpdatedByUserId { get; set; }
        //public DateTime? UpdatedOn { get; set; }
    }
}