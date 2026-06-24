namespace BRaVe_Mobile_Backend.Models
{
    public class CustomKeyPair
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; } = true;
        public int? LookupId { get; set; }
    }
}