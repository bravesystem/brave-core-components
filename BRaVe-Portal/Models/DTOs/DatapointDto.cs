using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BRaVe_Portal.Models.DTOs
{
    public class DataPointDto
    {
   
        public int? ProgramId { get; set; }
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int? DataPointOrder { get; set; }
        public int CategoryId { get; set; }
        public int AnswerType { get; set; }
        //public int? QuestionOrder { get; set; }
        public string? DefaultLang { get; set; }
        public int? LookupId { get; set; }
        public int? DatasetId { get; set; }
        public short? MinSelection { get; set; }
        public short? MaxSelection { get; set; }
        public string? Restriction { get; set; }
        public string? SkipLogic { get; set; }
        public string? ResultExpression { get; set; }
        public bool IsActive { get; set; }
        public bool IsRequired { get; set; }
        public string? QuestionText { get; set; }

        // Default language translation text (for initial insert)

        public string? Text { get; set; }
        public string? Description { get; set; }


        }
}
