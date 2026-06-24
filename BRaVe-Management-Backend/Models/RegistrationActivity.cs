using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace BRaVe_Management_Backend.Models
{
    public class RegistrationActivity
    {
        public int? ActivityId { get; set; }
        public string? ActivityCode { get; set; }
        public int ProgramId { get; set; }
        public int TenantId { get; set; }

        public string Description { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? RestrictToEnumerator { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; } 
        public int StatusId { get; set; }
        public bool Deleted { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; } 
        public string? UpdatedByUserId { get; set; } = string.Empty;
        public DateTime? UpdatedOn { get; set; }

        //to restrict activity
        public bool IsActive { get; set; } = false;
        public bool AllowRegistration { get; set; } = true;
        public bool AllowBiometricRecordCheck { get; set; } = true;

        //admin level info, not the gps coordinates
        public string? JsonAdminAreas { get; set; }
        public AdminArea? AdminArea { get; set; } = new AdminArea();            
        public List<ActivityDataPoint> DataPoints { get; set; } = new();
        public List<ActivityDistributions> Distributions { get; set; } = new();
        public List<ActivitySurveys>? Surveys { get; set; }
        public List<ActivityConsent> Consents { get; set; } = new();
        public List<ActivityPreference> Preferences { get; set; } = new();
        public List<ActivityEnumerator> Enumerators { get; set; } = new();

    }

    public class AdminArea {
        public Dictionary<int, int?> SelectedLocationIds { get; set; } = new();
        public string? AddressInfo { get; set; }

    }

    public class ActivityConsent
    {
        public int ActivityId { get; set; }
        public int ConsentId { get; set; }
        public int ConsentType { get; set; }
        public int TenantId { get; set; }
        public string Title { get; set; }
        public string Description{ get; set;}
        public bool Required { get; set; }
    }

    public class ActivitySurveys
    {
        public int SurveyId { get; set; }    
        public int ActivityId { get; set; }        
        public int TenantId { get; set; }        
        public string Title { get; set; } = string.Empty;
        public bool IsRequired { get; set; }       
        public bool IsActive { get; set; }
        public int SurveyType { get; set; }
    }
    public class ActivityPreference
    {

        public int PreferenceId { get; set; }
        public int ActivityId { get; set; }
        public int TenantId { get; set; }
        public int PreferenceType { get; set; }
        public string DefaultValue { get; set; } = string.Empty;
        public string PreferenceName { get; set; } = string.Empty;


    }
    public class ActivityEnumerator
    {
        public int ActivityId { get; set; }
        public int EnumeratorId { get; set; }
        public string EnumeratorCode { get; set; }
        public string? FullName { get; set; }
        public bool Required { get; set; }
        public bool IsActive { get; set; }
    }

    public class ActivityDistributions
    {
        public int ActivityId { get; set; }
        public int DistributionId { get; set; }
        public int TenantId { get; set; }
        public int DistributionType { get; set; }
        //public string Code { get; set; }
        public string note { get; set; }
        public string Name { get; set; }
        public bool? Photo { get; set; }
        public bool? Biometric { get; set; }
        public string? Mode { get; set; }
    }

}

