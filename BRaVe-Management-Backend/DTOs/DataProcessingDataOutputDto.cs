namespace BRaVe_Management_Backend.DTOs
{
    public class DataProcessingDataOutputDto
    {
        public int AssessmentId { get; set; }
        public int RecipientId { get; set; }
        public string? RecipientOfOutput { get; set; }
        public string? DataOutputAndUsageJustification { get; set; }

    }
}
