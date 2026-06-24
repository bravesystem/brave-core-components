namespace BRaVe_Management_Backend.Models
{
    public class DataProcessingSourceOfData
    {
        public int AssessmentId { get; set; }
        public int PrimarySourceId { get; set; }
        public string? PrimarySourceOther { get; set; }
        public int? SecondarySourceId { get; set; }
        public string? SecondarySourceOther { get; set; }
        public string? AttachmentUrl { get; set; }

        
    }
}
