using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace BRaVe_Portal.Models.DTOs
{
    public class TargetingRuleDto
    {
        public int Id { get; set; }
        public int? ProgramId { get; set; }
        public string? Code { get; set; }
        public int TenantId { get; set; }
        public bool WeightBound { get; set; }
        public bool IsValid { get; set; }
        public bool IsCustom { get; set; } = true;
        public string? RuleName { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public decimal? TotalWeight { get; set; }
        public string? RuleJson { get; set; } = default!;
        public RuleGroupDto? Criteria { get; set; }
    }
 }