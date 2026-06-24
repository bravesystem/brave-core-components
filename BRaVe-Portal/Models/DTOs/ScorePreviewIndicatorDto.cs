using BRaVe_Portal.Interfaces;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BRaVe_Portal.Models.DTOs
{

    public class ScorePreviewIndicatorDto
    {
        public string IndicatorCode { get; set; } = null!;

        public decimal IndicatorValue { get; set; }

        public decimal ScoreAwarded { get; set; }
    }
}