using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;
using System.Text.Json;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface ICompositeEvaluatorService
    {
        decimal? Evaluate(CompositeExpressionNode node, Dictionary<string, decimal?> values);
    }
}
