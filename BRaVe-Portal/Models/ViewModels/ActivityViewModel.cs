using System.ComponentModel.DataAnnotations;
using BRaVe_Portal.Models.Enums;

namespace BRaVe_Portal.Models.ViewModels
{
    public class ActivityViewModel
    {
        public int? ActivityId { get; set; }
        public string? ActivityCode { get; set; }
        public int ProgramId { get; set; }
        public int TenantId { get; set; }

      
        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Start date is required")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        public bool RestrictToEnumerator { get; set; }

        [Required(ErrorMessage = "Latitude is required")]
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
        public decimal latitude { get; set; }

        [Required(ErrorMessage = "Longitude is required")]
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
        public decimal Longitude { get; set; } 

        public int StatusId { get; set; }
        public bool Deleted { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public string? UpdatedByUserId { get; set; } = string.Empty;
        public DateTime? UpdatedOn { get; set; }


        public bool IsActive { get; set; } = false;
        public bool AllowRegistration { get; set; } = true;
        public bool AllowBiometricRecordCheck { get; set; } = true;

        public AdminArea? AdminArea { get; set; }

        /*public AssessmentStatus Status => (AssessmentStatus)StatusId;

        public bool IsReadOnly =>
            Status != AssessmentStatus.Draft &&
            Status != AssessmentStatus.PendingReview;*/
    }

    public class AdminArea
    {
        //public AdminArea(Dictionary<int, int?> selectedLocationIds, string? addressInfo)
        //{
        //    SelectedLocationIds = selectedLocationIds;
        //    AddressInfo = addressInfo;
        //}

        public Dictionary<int, int?> SelectedLocationIds { get; set; } = new();
        public string? AddressInfo { get; set; }

    }
}
