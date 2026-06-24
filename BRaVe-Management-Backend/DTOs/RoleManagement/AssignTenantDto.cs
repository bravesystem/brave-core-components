namespace BRaVe_Management_Backend.DTOs.RoleManagement
{

    public class AssignTenantDto
    {
        public string UserId { get; set; }
        public int TenantId { get; set; }
        public string CreatedByUserId { get; set; }
    }

}
