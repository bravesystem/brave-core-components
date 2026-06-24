using BRaVe_Portal.Models.Enums;

namespace BRaVe_Portal.Models
{
    public class Deduplication
    {
        public string firstName { get; set; }
        public string middleName { get; set; }
        public string lastName { get; set; }
        public int age { get; set; }
        public int GenderId { get; set; }  //1 for male, 2 for female
        public int raceid { get; set; }  //1 for male, 2 for female
        public int? TenantId { get; set; }

    }
}
