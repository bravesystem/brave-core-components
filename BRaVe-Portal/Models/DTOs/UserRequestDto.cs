namespace BRaVe_Portal.Models.DTOs
{
    public class UserRequestDto
    {
        public string UserId { get; set; }
        public string ProfileName { get; set; }

        public string? UserDetails { get; set; }

        public string Justification { get; set; }
    }
}
