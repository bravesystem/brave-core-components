namespace BRaVe_Portal.Models
{
    public class ActivityEnumerator
    {
        public int ActivityId { get; set; }
        public int TenantId { get; set; }
        public int EnumeratorId { get; set; }
        public string EnumeratorCode { get; set; }
        public string? FullName { get; set; }
        public bool Required { get; set; }
        public bool IsActive { get; set; }
    }
    
}

