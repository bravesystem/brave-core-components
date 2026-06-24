namespace BRaVe_Management_Backend.DTOs.claim_session
{
    public class PrintSessionRecordCreate
    {
        public int TenantId { get; set; }
        public string Label { get; set; } = "";
        public byte[] SessionCodeHash { get; set; } = Array.Empty<byte>();
        public int MaxDevices { get; set; }
    }
}
