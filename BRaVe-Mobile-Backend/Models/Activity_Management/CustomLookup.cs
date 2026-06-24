namespace BRaVe_Mobile_Backend.Models
{
    public class CustomLookup
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; } = true;
        public List<CustomKeyPair> Values { get; set; }


    }
}