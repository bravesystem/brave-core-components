namespace BRaVe_Portal.Models
{
    public class ActivityDataPoint
    {
        public int ActivityId { get; set; }
        public int DataPointId { get; set; }
        public int DatapointType { get; set; }
        public int TenantId { get; set; }
        public bool Required { get; set; }
    }
}

