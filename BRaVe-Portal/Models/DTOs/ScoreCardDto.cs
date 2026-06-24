using BRaVe_Portal.Interfaces;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BRaVe_Portal.Models.DTOs
{

    public class ScoreCardDto
    {
        public int ScoreCardId { get; set; }

        public string Code { get; set; } = null!;

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        // Program / Mission this belongs to
        public int ProgramId { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedOn { get; set; }

        public List<ScoreCardRuleDto> Rules { get; set; } = new();
    }
}