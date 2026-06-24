namespace BRaVe_Management_Backend.Models
{
    public class DataProcessingDataOutput
    {
        public int AssessmentId { get; set; }
        public int DataOutputId { get; set; }
        public int RecipientId { get; set; }
        public string? RecipientOfOutput { get; set; }
        public string? DataOutputAndUsageJustification { get; set; }

    }
}
