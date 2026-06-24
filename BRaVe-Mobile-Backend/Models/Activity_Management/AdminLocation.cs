namespace BRaVe_Mobile_Backend.Models
{
    public class AdminLocation
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int level { get; set; }
        public int? parent { get; set; }
        public bool IsActive { get; set; }
    }
}