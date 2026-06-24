using BRaVe_Portal.Models.DTOs;

namespace BRaVe_Portal.Models.ViewModels
{
    public class MissionViewModel
    {
        public MissionDto mission { get; set; }
        public List<LookupItemDto> FocalPoints { get; set; }
    }
}
