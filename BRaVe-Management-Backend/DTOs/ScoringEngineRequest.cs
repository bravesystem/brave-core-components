using BRaVe_Management_Backend.Models;

namespace BRaVe_Management_Backend.DTOs
{
    public class TargetingRequest
    {
        public DateRangeDto range { get; set; }
        //public RuleDefinition criteria { get; set; }
        public int targetingId { get; set; }

    }
}
