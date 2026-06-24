namespace BRaVe_Management_Backend.DTOs
{
    public sealed class CreateSessionRequest
    {
        //public int TenantId { get; set; }
        public int MaxClaims { get; set; }
        public int? TtlMinutes { get; set; }      // e.g., 120
        public int? PolicyVersion { get; set; } = 1;   // default 1
        public string? Label { get; set; }
    }

}
