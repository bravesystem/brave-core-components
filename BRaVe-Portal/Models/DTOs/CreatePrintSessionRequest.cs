namespace BRaVe_Portal.Models.DTOs
{
    public class CreatePrintSessionRequest
    {
        public string? Label { get; set; }
        public int MaxDevices { get; set; }
    }
}
