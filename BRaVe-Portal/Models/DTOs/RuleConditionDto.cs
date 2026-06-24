using BRaVe_Portal.Interfaces;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace BRaVe_Portal.Models.DTOs
{
    public class RuleConditionDto 
    {
        public string Field { get; set; } = default!;
        public string Operator { get; set; } = default!;
        public object Value { get; set; } = default!;
    }
    }