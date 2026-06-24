namespace BRaVe_Portal.Models
{
    public class ActivityPreference
    {
        public int PreferenceId { get; set; }
        public int ActivityId { get; set; }
        public int TenantId { get; set; }
        public int PreferenceType { get; set; }
        public string DefaultValue { get; set; } = string.Empty;
        public string PreferenceName { get; set; } = string.Empty;
    }
    
}

