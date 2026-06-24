namespace BRaVe_Mobile_Backend.DTOs
{
    public sealed class ActivateResponse
    {
        public string DeviceId { get; set; }
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public DateTimeOffset AccessTokenExpiresAt { get; set; }
    }
}
