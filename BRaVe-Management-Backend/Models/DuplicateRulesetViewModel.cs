namespace BRaVe_Management_Backend.Models
{
    public class DuplicateRulesetViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
        public int? TenantId { get; set; }
    }
}
