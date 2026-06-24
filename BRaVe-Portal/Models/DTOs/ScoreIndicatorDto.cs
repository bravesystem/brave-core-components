using BRaVe_Portal.Interfaces;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BRaVe_Portal.Models.DTOs
{

    public class ScoreIndicatorDto
    {
        public int IndicatorId { get; set; }

        public string Code { get; set; } = null!;

        public string Name { get; set; } = null!;

        // Serialized CompositeExpressionNode
        public string JsonRule { get; set; } = null!;

        // ⭐ THIS is what your engine needs
        public decimal Weight { get; set; }
    }
}