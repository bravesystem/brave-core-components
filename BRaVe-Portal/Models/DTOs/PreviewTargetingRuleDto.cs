using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace BRaVe_Portal.Models.DTOs
{
    public class PreviewTargetingRuleDto
    {
        public RuleGroupDto Criteria { get; set; } = default!;
    }
}