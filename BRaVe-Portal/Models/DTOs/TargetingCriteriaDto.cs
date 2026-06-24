using BRaVe_Management_Backend.DTOs;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace BRaVe_Portal.Models.DTOs
{
    public class TargetingCriteriaDto
    {
        public int Id { get; set; }

        // FK
        public int TargetingId { get; set; }

        public string? Code { get; set; }

        public string CriteriaName { get; set; }

        public string? Description { get; set; }

        public DateTime CreatedOn { get; set; }

        public string? CreatedBy { get; set; }

        // Scoring
        public decimal? Score { get; set; }

        public decimal? Weight { get; set; }

        // Rules
        public string? JsonRule { get; set; }

        public string? Expression { get; set; }

        public IndicatorDataType DataType { get; set; }

        public int? LookUpId { get; set; }

        // Parsed rule
        public RuleGroupDto? Criteria { get; set; }

        public List<TargetingFieldValueDto> LookupValues { get; set; } = new();
    }

}