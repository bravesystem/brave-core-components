namespace BRaVe_Portal.Models.DTOs
{
    public class MissionDto
    {
        public int MissionId { get; set; }
        public string CountryIso2 { get; set; }
        public string Name { get; set; }
        public int FocalpointType { get; set; }
        public string? Note { get; set; }
    }
}
