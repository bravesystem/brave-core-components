namespace BRaVe_Mobile_Backend.Models
{
    public class SetPinRequest
    {
        public string DeviceId { get; set; }   
        public string? RequestIp { get; set; }   
        public int TenantId { get; set; }
        public string Code { get; set; }
        public byte[] OldPin { get; set; }
        //public bool IsDefault { get; set; }
        public byte[] NewPin { get; set; }     
    }
}
