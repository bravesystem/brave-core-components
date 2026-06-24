namespace BRaVe_Portal.Models.ViewModels
{
    public class ActivityDefaultPrefferenceViewModel
    {
        public int Id { get; set; }
        public int ActivityId { get; set; }
        public int ProgramId { get; set; }
        public string? SettingName { get; set; }
        public bool Required { get; set; }

    }
}