namespace BRaVe_Management_Backend.DTOs
{
    public class EnumeratorDto
    {
        public int EnumeratorId { get; set; }
        public string? FullName { get; set; }
        public string? Note { get; set; }

        public string? EnumeratorCode { get; set; }
        public string? EnumeratorType { get; set; }
        public string? EnumeratorPin { get; set; }

        public bool IsPinUpdated { get; set; }
        public bool IsSupervisor { get; set; } = false;
        public bool IsActive { get; set; } = true;
    }
}
