namespace BRaVe_Management_Backend.Models
{
    public class Rule
    {
        /// <summary>
        /// Condition that must be satisfied for this rule to apply
        /// </summary>
        public RuleCondition When { get; set; } = default!;

        /// <summary>
        /// Score to add if condition is satisfied
        /// </summary>
        public int Score { get; set; }
    }
}
