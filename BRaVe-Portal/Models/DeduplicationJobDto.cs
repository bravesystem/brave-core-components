using BRaVe_Portal.Models.DTOs;

namespace BRaVe_Portal.Models
{
    public class DeduplicationJobDto
    {
        public long JobId { get; set; }
        public int RuleId { get; set; }
        public string RulesetName { get; set; } = string.Empty;
        public int StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
        public DateTime? CompletedOn { get; set; }
        public int Order => StatusId <= 2 ? 0 : 1;
    }

    public class PagedDeduplicationJobsDto
    {
        public bool Success { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public List<DeduplicationJobDto> Data { get; set; } = new();
    }

    //public record PagedDeduplicationJobsDto(int TotalRecords, int TotalPages, List<DeduplicationJobDto> Jobs);

}
