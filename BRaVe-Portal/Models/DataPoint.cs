using BRaVe_Portal.Helpers;
using BRaVe_Portal.Models.Enums;
using StackExchange.Redis;

namespace BRaVe_Portal.Models
{
    public class Datapoint    {
        public int Id { get; set; }
        public int ProgramId { get; set; }
        public int? TenantId { get; set; }
        public int? CategoryId { get; set; }
        public int AnswerType { get; set; }
        public int? LookupId { get; set; }
        public int? DatasetId { get; set; }
        public short? MinSelection { get; set; }
        public short? MaxSelection { get; set; }
        public string? Restriction { get; set; }
        public bool? IsActive { get; set; }
        public string CreatedByUserId { get; set; }
        public string UpdatedByUserId { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }

       
    }
}
