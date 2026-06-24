namespace BRaVe_Portal.Models.ViewModels
{
    public class DataPointViewModel
    {

        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string Text { get; set; }
        public int AnswerType { get; set; }
        public int? LookupId { get; set; }
        public int? DatasetId { get; set; }
        public short? MinSelection { get; set; }
        public short? MaxSelection { get; set; }
        public string? Restriction { get; set; }
        public string CreatedByUserId { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public string? UpdatedByUserId { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? QuestionText { get; set; }

        public bool IsActive { get; set; }

        public int? DataPointOrder { get; set; }

    }
}