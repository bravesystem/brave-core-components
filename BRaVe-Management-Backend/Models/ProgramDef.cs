using BRaVe_Management_Backend.DTOs;
using Microsoft.AspNetCore.Http.HttpResults;
using System;

namespace BRaVe_Management_Backend.Models
{
    public class ProgramDef
    {
        public int ProgramId { get; set; }
        public string ProgramCode { get; set; }
        public int? AssessmentId { get; set; }
        public int TenantId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedOn { get; set; } 
        public DateTime? UpdatedOn { get; set; } 
        public string? UpdatedByUserId { get; set; }
        public int StatusId { get; set; }
        public List<UserProgAssignmentDto> UserProgAssignments { get; set; } = new();


    }
}
