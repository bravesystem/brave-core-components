namespace BRaVe_Mobile_Backend.DTOs
{
    public class TokenRefreshDto
    {
        public string DeviceId { get; set; }
        public string Enumerator { get; set; }
        public byte[] TokenHash { get; set; }
        public byte[] FreshToken { get; set; }
        public string FreshRaw { get; set; }
    }
}
