namespace BRaVe_Portal.Models.DTOs
{
    public class ProgrammaticDataDto
    {
        public int Id { get; set; }

        public Dictionary<string, object?> Fields { get; set; } = new Dictionary<string, object?>();
    }

}
