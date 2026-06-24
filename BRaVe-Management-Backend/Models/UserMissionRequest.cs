namespace BRaVe_Management_Backend.Models
{
    public class UserMissionRequest
    {

        public int Id { get; set; }

        public string UserId { get; set; } 
        public string ProfileName { get; set; } 

        public string? UserDetails { get; set; }

        public string Justification { get; set; }

        public DateTime RequestedOn { get; set; } = DateTime.UtcNow;

        public bool IsProcessed { get; set; } = false;

        public string? ProcessedByUserId { get; set; }

        public DateTime? ProcessededOn { get; set; }

    }
}
