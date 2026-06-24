using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace BRaVe_Portal.Models.DTOs
{
    public class ColumnDto
    {
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    } }