namespace BRaVe_Management_Backend.DTOs
{
    public class TargetingFieldValueDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;// This will store LookupValueId
        public string Name { get; set; }

    }

}
