using BRaVe_Portal.Interfaces;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BRaVe_Portal.Models.DTOs
{

    public class ScorePreviewResultDto
    {
        public string EntityId { get; set; } = null!;

        public decimal TotalScore { get; set; }

        public bool Eligible { get; set; }

        public List<ScorePreviewIndicatorDto> Indicators { get; set; } = new();
    }
}