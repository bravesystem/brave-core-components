namespace BRaVe_Management_Backend.DTOs.PrintService
{
    public class AuthorizeReprintRequest
    {
        public int CardId { get; set; }
        public string Reason { get; set; }
        public string? AuthorizedBy { get; set; }
    }
}
