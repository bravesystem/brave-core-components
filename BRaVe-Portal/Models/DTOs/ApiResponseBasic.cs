using System.Text.Json.Serialization;

namespace BRaVe_Portal.Models.DTOs
{
    public class ApiResponseBasic
    {
        public bool success { get; set; } = false;

        [JsonPropertyName("errorcode")]
        public int errorCode { get; set; } = 0;

        [JsonPropertyName("message")]
        public string errorMessage { get; set; } = null;
    }
}
