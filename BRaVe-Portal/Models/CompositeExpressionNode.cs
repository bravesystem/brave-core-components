using System.ComponentModel.DataAnnotations;

namespace BRaVe_Portal.Models
{
   
    public class CompositeExpressionNode
    {
        public string Op { get; set; }
        public object? Value { get; set; }   // <-- change from decimal? to object?
        public string? Code { get; set; }
        public CompositeExpressionNode? Left { get; set; }
        public CompositeExpressionNode? Right { get; set; }
        public CompositeExpressionNode? Fallback { get; set; }
    }

    
}
