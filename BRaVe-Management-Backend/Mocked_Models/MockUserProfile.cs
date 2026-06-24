namespace BRaVe_Management_Backend.Mocked_Models
{
    public class MockUserProfile
    {
        public string sub { get; set; } = Guid.NewGuid().ToString("N");
        public string oid { get; set; } = Guid.NewGuid().ToString();
        public string tid { get; set; } = Guid.NewGuid().ToString();
        public string preferred_username { get; set; } //= "user@example.com";
        public List<string> emails { get; set; } //= new() { "user@example.com" };
        public string name { get; set; } = "Demo User";
        public string given_name { get; set; } = "Demo";
        public string family_name { get; set; } = "User";
        public List<string> roles { get; set; } = new();
        public List<string> groups { get; set; } = new();
        public Dictionary<string, string> extra { get; set; } = new();
    }
}
