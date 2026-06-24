using System.Text.Json.Serialization;

namespace BRaVe_Management_Backend.Models
{
    public class Mission
    {
        public int MissionId { get; set; }
        public string CountryIso2 { get; set; }
        public string TenantCode { get; set; }
        public string Name { get; set; }

        public int FocalpointType { get; set; }
        public string? Note { get; set; }


    }
}
