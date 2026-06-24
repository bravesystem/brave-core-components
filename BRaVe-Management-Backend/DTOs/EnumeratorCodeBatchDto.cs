namespace BRaVe_Management_Backend.DTOs
{
    public class EnumeratorCodeBatchDto
    {
        public int TenantId { get; set; }

        public string Prefix { get; set; }

        public int SuffixLength { get; set; }

        public int TotalCodeGenerated { get; set; }

    }
}
