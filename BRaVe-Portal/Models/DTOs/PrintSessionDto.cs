namespace BRaVe_Portal.Models.DTOs
{
    public class PrintSessionDto
    {
        public long SessionId { get; set; }
        public string Label { get; set; } = "";
        public int MaxDevices { get; set; }
        public int DevicesConnected { get; set; }
        public int Status { get; set; }
        public DateTime CreatedOnUtc { get; set; }
    }
}
