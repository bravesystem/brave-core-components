namespace BRaVe_Mobile_Backend.DTOs
{
    public sealed class ClaimResponse
    {
        public string HouseholdPrefix { get; set; } = "";
        public int InitialId { get; set; } = 1;
        public string JwsToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public string PubRsaKeyB64 { get; set; } = "";
    }
}
