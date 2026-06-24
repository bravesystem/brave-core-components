namespace BRaVe_Management_Backend.Interfaces.jobs
{
    public class TargetingJob
    {
        public long Id { get; set; }
        public int TenantId { get; set; }
        public int TargetingId { get; set; }
        public string? TargetingRule { get; set; }
        public DateTime StartPeriod { get; set; }
        public DateTime EndPeriod { get; set; }
        public string CreatedBy { get; set; }
        public int StatusId { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
        public DateTime? CompletedOn { get; set; }

        public int Order => StatusId == 2 ? 0 : 1;
    }

    public record PagedTargetingJobsResult(int TotalRecords, int TotalPages, List<TargetingJob> Jobs);

    public static class TargetingJobStatus
    {
        public const int Queuing = 1;
        public const int Running = 2;
        public const int Completed = 3;
        public const int CompletedDisabled = 4;
        public const int Failed = 5;
    }
}
