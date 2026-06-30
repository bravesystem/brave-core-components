namespace BRaVe_Management_Backend.DTOs
{
    public class FileBasedDeduplicationProcessResult
    {
        public Guid JobId { get; init; }

        public string Message { get; init; } = string.Empty;
    }
}
