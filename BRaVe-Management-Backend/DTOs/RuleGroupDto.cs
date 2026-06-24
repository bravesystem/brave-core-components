using BRaVe_Management_Backend.Interfaces;

namespace BRaVe_Management_Backend.DTOs
{
    public class RuleGroupDto : IRuleItem
    {
        public string Combinator { get; set; } = "AND";
        public List<object> Rules { get; set; } = new();
    }
}