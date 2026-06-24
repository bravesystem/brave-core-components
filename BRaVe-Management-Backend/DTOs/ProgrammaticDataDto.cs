namespace BRaVe_Management_Backend.DTOs
{
    public class ProgrammaticDataDto
    {
        public int Id { get; set; }

        public Dictionary<string, object?> Fields { get; set; }= new Dictionary<string, object?>();
    }

}
