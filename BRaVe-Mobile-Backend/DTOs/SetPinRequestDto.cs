using System.Text.Json.Serialization;

namespace BRaVe_Mobile_Backend.DTOs
{
    public class SetPinRequestDto
    {
        public string code { get; set; }
        public string oldPin { get; set; }
        public string newPin { get; set; }

    }
}
