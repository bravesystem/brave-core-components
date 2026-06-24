namespace BRaVe_Management_Backend.DTOs.PrintService
{
    public class SyncPrintedRequest
    {
        public List<string> HouseholdIds { get; set; }
        public string DeviceId { get; set; }
        public string WindowsUser { get; set; }
    }
}
