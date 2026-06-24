using BRaVe_Portal.Helpers;
using BRaVe_Portal.Models.Enums;
using Microsoft.AspNetCore.Http.HttpResults;
using StackExchange.Redis;
using System.Security.Principal;

namespace BRaVe_Portal.Models
{
    public class MissionPreferences
    {
        public int PreferenceId { get; set; }
        public int ProgramId { get; set; }
        public string PreferenceName { get; set; }
        public string DefaultValue { get; set; }
        public int? TenantId { get; set; }
        public string? Description { get; set; }
        public string PreferenceType { get; set; }
        public string CreatedByUserId { get; set; }
        public string UpdatedByUserId { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }

        public bool IsFromDefault { get; set; } = false;
    }
}


