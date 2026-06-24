namespace BRaVe_Portal.Models.DTOs
{
    public class TargetingJobDto
    {
        public long Id { get; set; }
        public int TargetingId { get; set; }
        public string? TargetingRule { get; set; }
        public DateTime StartPeriod { get; set; }
        public DateTime EndPeriod { get; set; }
        public string CreatedBy { get; set; }
        public int StatusId { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? CompletedOn { get; set; }

        public int Order => StatusId <= 2 ? 0 : 1;


    }

}
