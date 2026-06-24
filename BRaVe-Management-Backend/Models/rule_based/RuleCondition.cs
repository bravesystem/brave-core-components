namespace BRaVe_Management_Backend.Models
{
    public class RuleCondition
    {
        /// <summary>
        /// All conditions must be true (AND)
        /// </summary>
        public List<AtomicCondition>? All { get; set; }

        /// <summary>
        /// At least one condition must be true (OR)
        /// </summary>
        public List<AtomicCondition>? Any { get; set; }
    }
}
