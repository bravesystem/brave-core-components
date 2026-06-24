namespace BRaVe_Management_Backend.DTOs
{
 
    public class CompositeIndicatorDto
    {
        public int CompositeId { get; set; }
        public int IndicatorId { get; set; }
        public CompositeExpressionNode Expression { get; set; } = default!;
        public int CalculationOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }


    public class CompositeExpressionNode
    {
        public string Op { get; set; } = default!;

        // for indicator references
        public string? Code { get; set; }

        // literal values
        public decimal? Literal { get; set; }

        // tree structure
        public CompositeExpressionNode? Left { get; set; }
        public CompositeExpressionNode? Right { get; set; }

        // for coalesce / fallback
        public CompositeExpressionNode? Value { get; set; }
        public decimal? Fallback { get; set; }
    }

}
