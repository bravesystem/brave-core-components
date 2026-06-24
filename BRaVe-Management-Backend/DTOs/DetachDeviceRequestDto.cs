namespace BRaVe_Management_Backend.DTOs
{
    public class DetachDeviceRequestDto
    {
        public string DeviceId { get; set; }
        public int CurrentTenantId { get; set; }
        public int OldTenantId { get; set; }
        public string DetachReason { get; set; } = string.Empty;
    }
}
