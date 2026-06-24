using System.Collections.Generic;

namespace BRaVe_Portal.Models.ViewModels
{
    public class DataProcessingSecurityMeasure
    {
        public int AssessmentId { get; set; }
        public int SecurityMeasureId { get; set; } //show select list with dummy data
        public string? TechnicalMeasures { get; set; }
        public string? OrganizationalMeasures { get; set; }
    }
}