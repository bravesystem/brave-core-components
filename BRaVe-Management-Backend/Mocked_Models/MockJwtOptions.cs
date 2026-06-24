namespace BRaVe_Management_Backend.Mocked_Models
{
    public class MockJwtOptions
    {
        public bool Enabled { get; set; }
        public string Issuer { get; set; } = "";
        public string Audience { get; set; } = "";
        public string SigningKey { get; set; } = "";
        public int AccessTokenMinutes { get; set; } = 60;
        public MockUserProfile UserProfile { get; set; } = new();
    }
}
