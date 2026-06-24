namespace BRaVe_Management_Backend.DTOs
{
    public class ConsentDto
    {
       // public int Id { get; set; }
        //public int TenantId { get; set; }
        public int ProgramId { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public string? DefaultLang { get; set; }

    }
}
