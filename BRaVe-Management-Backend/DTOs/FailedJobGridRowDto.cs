namespace BRaVe_Management_Backend.DTOs
{


    public class FailedJobGridRowDto
    {
        public Guid BatchId { get; set; }
        public string Mission { get; set; } = string.Empty;
        public DateTime SyncAttemptOn { get; set; }
        public string DeviceId { get; set; }

    }
}