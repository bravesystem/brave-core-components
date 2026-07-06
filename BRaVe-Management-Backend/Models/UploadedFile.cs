namespace BRaVe_Management_Backend.Models
{
    public class UploadedFile
    {
        public Guid JobId { get; set; }
        public string Filename { get; set; }
        public string UploadedBy { get; set; } = string.Empty;
        public DateTime UploadedOn { get; set; }
        public bool IsProcessed { get; set; } = false;
        public DateTime? ProcessedOn { get; set; }
    }
}
