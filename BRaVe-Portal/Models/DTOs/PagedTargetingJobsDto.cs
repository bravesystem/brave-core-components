namespace BRaVe_Portal.Models.DTOs
{
    public class PagedTargetingJobsDto
    {
        public bool Success { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public List<TargetingJobDto> Data { get; set; } = new();
    }

}
