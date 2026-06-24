namespace BRaVe_Portal.Models.ViewModels
{
    public class DuplicateRulesetViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
        public int? TenantId { get; set; }
        public bool IsBuiltIn => TenantId == null;
    }
}
