using BRaVe_Portal.Interfaces;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BRaVe_Portal.Models.DTOs
{

    public class RuleGroupDto
    {
        public string Combinator { get; set; } = "AND";

        // Inner rules can be either other groups or conditions
        public List<object> Rules { get; set; } = new();

        // Add this to match the JSON you already have
        public List<RuleConditionDto>? Conditions { get; set; }
    }
}