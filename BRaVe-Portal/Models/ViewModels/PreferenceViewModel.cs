using BRaVe_Portal.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BRaVe_Portal.Models.ViewModels
{
    public class PreferenceViewModel
    {
        public int PreferenceId { get; set; }
        public int ActivityId { get; set; }
        public string PreferenceName { get; set; } // e.g., "CollectBiometric"
        public int PreferenceType { get; set; }  //int, numeric, boolean, text
        public string DefaultValue { get; set; } // e.g., "True"   -- default value parse to a string, related to the selected type
        public string CreatedByUserId { get; set; }
        public bool IsRequired { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public string? UpdatedByUserId { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int ProgramId { get; set; }
        public int? TenantId { get; set; }
        public string? Description { get; set; }
       


        public List<Preference> AllPreferences { get; set; } = new List<Preference>();
        [BindProperty]
        public List<int> SelectedPreferenceIds { get; set; } = new();

        // Values entered for each preference (ID -> Value)
        [BindProperty]
        public Dictionary<int, string> PreferenceValues { get; set; } = new();

        // Program-specific info / common data
        [BindProperty]
        public MissionPreferencesDto UpdatedPreferences { get; set; } = new();



    }
}