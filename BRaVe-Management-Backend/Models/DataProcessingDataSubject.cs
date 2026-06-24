namespace BRaVe_Management_Backend.Models
{
    public class DataProcessingDataSubject
    {
        public int AssessmentId { get; set; }
        public int SubjectTypeId { get; set; }
        public bool IsVulnerableGroup { get; set; } = false;

    }
}
