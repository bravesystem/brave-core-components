namespace BRaVe_Portal.Models.DTOs
{
    public class MissionPreferencesDto
    {
        public int? Id { get; set; }
       
        public string PreferenceType { get; set; } = string.Empty;
        
        public int? TenantId { get; set; }
        public string? DefaultValue { get; set; }
        public string? CreatedByUserId { get; set; }
        public string? UpdatedByUserId { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }

    }
}
