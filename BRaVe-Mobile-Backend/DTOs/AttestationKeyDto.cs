namespace BRaVe_Mobile_Backend.DTOs
{
    public sealed class AttestationKeyDto
    {
        public List<string> ChainPem { get; set; } = new(); // leaf -> root
        public string Challenge { get; set; } = "";         // base64url or hex
    }
}
