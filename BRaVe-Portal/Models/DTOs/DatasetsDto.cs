using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace BRaVe_Portal.Models.DTOs
{
    public class DatasetsDto
    {
            public int Id { get; set; }

            public int TenantId { get; set; }
            public string Title { get; set; } = default!;
            public string? Description { get; set; }
            public string? SchemaJson { get; set; } = default!;
            public bool IsActive { get; set; } = true;
            public int Version { get; set; } = 1;
            public string? CreatedBy { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            public DateTime? UpdatedAt { get; set; }

            [NotMapped]
            public object? Schema
            {
                get => string.IsNullOrWhiteSpace(SchemaJson) ? null : JsonSerializer.Deserialize<object>(SchemaJson);
                set => SchemaJson = JsonSerializer.Serialize(value);
            }
        }
    }