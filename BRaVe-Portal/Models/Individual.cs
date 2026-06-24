using BRaVe_Portal.Models.Enums;

namespace BRaVe_Portal.Models
{
    public class Individual
    {
        public string last_name { get; set; }
        public string first_name { get; set; }
        public int age { get; set; }
        public int GenderId { get; set; }  //1 for male, 2 for female
        public int raceid { get; set; }  //1 for male, 2 for female
    }
}
