namespace BRaVe_Portal.Models.DTOs
{
    public class FileBasedDeduplicationProcessResult
    {
        public Guid JobId { get; init; }

        public string Message { get; init; } = string.Empty;
    }
}
