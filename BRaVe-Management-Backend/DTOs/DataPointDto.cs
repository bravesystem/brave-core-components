namespace BRaVe_Management_Backend.DTOs
{
    public class DataPointDto
    {
        public int? Id { get; set; }
        public int TenantId { get; set; }
        public int CategoryId { get; set; }
        public int AnswerType { get; set; }
        public int? LookupId { get; set; }
        public int? DatasetId { get; set; }
        public int? MinSelection { get; set; }
        public int? MaxSelection { get; set; }
        public string? Restriction { get; set; }
        public string? CreatedByUserId { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? UpdatedByUserId { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? Text { get; set; }
        public string? Description { get; set; }
        public string? DefaultLang { get; set; }


        public bool IsActive { get; set; }

    }
}
