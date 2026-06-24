using BRaVe_Management_Backend.Interfaces;

namespace BRaVe_Management_Backend.DTOs
{
    public class RuleConditionDto : IRuleItem
    {
        public string Field { get; set; } = default!;
        public string Operator { get; set; } = default!;
        public string Value { get; set; } = default!;
    }
}