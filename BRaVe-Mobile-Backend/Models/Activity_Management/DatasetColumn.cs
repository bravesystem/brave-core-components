namespace BRaVe_Mobile_Backend.Models
{
    public class DatasetColumn
    {
        public int Id { get; set; }
        public int Order { get; set; }
        public string Name { get; set; }
        public int Type { get; set; }
        public int? LookupId { get; set; }
        public bool IsActive { get; set; }
        public int DatasetId { get; set; }
    }
}