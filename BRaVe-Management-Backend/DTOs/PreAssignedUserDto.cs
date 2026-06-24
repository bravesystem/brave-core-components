namespace BRaVe_Management_Backend.DTOs
{
    public class PreAssignedUserDto
    {
        public string UserDetails { get; set; }
        public string MissionName { get; set; }
        public string RoleName { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }


    }
}
