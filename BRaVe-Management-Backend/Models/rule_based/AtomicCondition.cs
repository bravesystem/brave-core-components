namespace BRaVe_Management_Backend.Models
{
    public sealed class AtomicCondition
    {
        /// <summary>
        /// Direct field on Household_Document (e.g. Gender, Age)
        /// </summary>
        public string? Field { get; set; }

        /// <summary>
        /// Operator: =, >, <, >=, <=, !=
        /// </summary>
        public string Op { get; set; } = default!;

        /// <summary>
        /// Comparison value
        /// </summary>
        public string? Value { get; set; }

        /// <summary>
        /// Survey question id (used when Field is null)
        /// </summary>
        public int? Question { get; set; }

        /// <summary>
        /// Expected answer for the question
        /// </summary>
        public string? Answer { get; set; }
    }

}
