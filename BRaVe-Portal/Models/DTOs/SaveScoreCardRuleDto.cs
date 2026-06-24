using BRaVe_Portal.Interfaces;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BRaVe_Portal.Models.DTOs
{

    public class SaveScoreCardRuleDto
    {
        public int? RuleId { get; set; }

        public string IndicatorCode { get; set; } = null!;

        public string Operator { get; set; } = null!;

        public decimal? CompareValue { get; set; }

        public decimal Score { get; set; }

        public decimal? MinValue { get; set; }

        public decimal? MaxValue { get; set; }

        public int Priority { get; set; }

        public bool IsActive { get; set; }

    }
}