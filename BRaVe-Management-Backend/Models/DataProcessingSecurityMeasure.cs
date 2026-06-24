namespace BRaVe_Management_Backend.Models
{
    public class DataProcessingSecurityMeasure
    {
        public int AssessmentId { get; set; }
        public int SecurityMeasureId { get; set; }
        public string TechnicalMeasures { get; set; }
        public string OrganizationalMeasures { get; set; }
    }
}
