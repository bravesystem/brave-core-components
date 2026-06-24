using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Interfaces;
using Serilog;
using System.Security.Cryptography;
using System.Text;

namespace BRaVe_Management_Backend.Helpers
{
    public class CompositeEvaluator : ICompositeEvaluatorService
    {
        public decimal? Evaluate(CompositeExpressionNode node, Dictionary<string, decimal?> values)
        {
            return node.Op switch
            {
                "indicator" => values.GetValueOrDefault(node.Code!),
                "literal" => node.Literal,
                "divide" => SafeDivide(Evaluate(node.Left!, values), Evaluate(node.Right!, values)),
                "coalesce" => Evaluate(node.Value!, values) ?? node.Fallback,
                _ => throw new NotSupportedException(node.Op)
            };
        }

        private static decimal? SafeDivide(decimal? a, decimal? b)
        {
            if (a == null || b == null || b == 0) return null;
            return a / b;
        }
    }
}