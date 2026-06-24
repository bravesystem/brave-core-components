namespace BRaVe_Portal.Models
{
    public class ActivityDistributors
    {
        public int ActivityId { get; set; }
        public int DistributionId { get; set; }
        public int TenantId { get; set; }
        public int DistributionType { get; set; }
        //public string Code { get; set; }
        public string note { get; set; }
        public string Name { get; set; }
        public bool? Photo { get; set; }
        public bool? Biometric { get; set; }
        public string? Mode { get; set; }

    }

}

