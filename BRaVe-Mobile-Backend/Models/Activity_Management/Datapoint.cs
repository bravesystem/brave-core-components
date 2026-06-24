namespace BRaVe_Mobile_Backend.Models
{
    public class Datapoint
    {
        public int Id { get; set; }
        public int Order { get; set; }
        public string DefaultLanguage { get; set; }
        public bool IsRequired { get; set; }
        public int Type { get; set; }
        public int? Lookup { get; set; }
        public int? Dataset { get; set; }
        public int? MinVal { get; set; }
        public int? MaxVal { get; set; }
        public string Restriction { get; set; }
        public List<Translation> Texts { get; set; } = new();
    }
}