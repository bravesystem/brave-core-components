namespace BRaVe_Management_Backend.DTOs
{
    public class AdministrativeLevelDto
    {
        public int Id { get; set; }
        public int TenantId { get; set; }

        public string LevelName { get; set; }
        public string? OfficialCode { get; set; } = "";

        public bool IsActive { get; set; } = false;
        public DateTime? UpdatedOn { get; set; }

     }
}
