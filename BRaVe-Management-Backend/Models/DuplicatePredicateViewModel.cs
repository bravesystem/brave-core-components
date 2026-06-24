namespace BRaVe_Management_Backend.Models
{
    public class DuplicatePredicates
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Fields { get; set; } = string.Empty;
        public int OperationCode { get; set; }
        public string OperationName { get; set; } = string.Empty;
        public decimal? Value { get; set; }
        public decimal Score { get; set; }
        public int? TenantId { get; set; }
    }

    public class DuplicatePredicateViewModel
    {
        public int RulesetId { get; set; }
        public string RulesetName { get; set; }
        public int? TenantId { get; set; }
        public List<DuplicatePredicates> Predicates { get; set; } = new();

    }

    public class PredicateEvaluationViewModel
    {
        public List<PredicateEvaluation> Evaluations { get; set; } = new();
    }

    public class PredicateEvaluation
    { 
        public DuplicateRule rule { get; set; }
        public bool EvaluationResult { get; set; }
    }
}
