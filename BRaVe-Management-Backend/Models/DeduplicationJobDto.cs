namespace BRaVe_Management_Backend.Models
{
    public class DeduplicationJobDto
    {
        public long JobId { get; set; }
        public int TenantId { get; set; } //new
        public int RuleId { get; set; }
        public string RulesetName { get; set; } = string.Empty;

        public DateTime StartPeriod { get; set; } //new
        public DateTime EndPeriod { get; set; } //new
        public int StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
        public DateTime? CompletedOn { get; set; }
    }

    public record DeduplicationJobRequest(int RuleId, DateTime StartPeriod, DateTime EndPeriod);

    public record PagedDeduplicationJobsDto(int TotalRecords, int TotalPages, List<DeduplicationJobDto> Jobs);

}
