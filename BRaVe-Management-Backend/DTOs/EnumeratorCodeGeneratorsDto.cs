namespace BRaVe_Management_Backend.DTOs
{
    public class EnumeratorCodeGeneratorsDto
    {
        public int TenantId { get; set; }

        public string Prefix { get; set; }

        public int SuffixLength { get; set; }

        public int? TotalCodeGenerated { get; set; }

        public string? LastCodeGenerated { get; set; }

        public int LatestId { get; set; }

        public string CreatedByUserId { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }
}
