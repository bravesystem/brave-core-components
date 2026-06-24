using BRaVe_Mobile_Backend.Interfaces;

namespace BRaVe_Mobile_Backend.Services
{
    public class AuthModeService : IAuthModeService
    {
        //private readonly IConfiguration _config;
        //public AuthModeService(IConfiguration config) => _config = config;
        //public bool IsMockMode() => _config["Auth:Mode"]?.Equals("Mock", StringComparison.OrdinalIgnoreCase) ?? false;

        private bool _mockMode;

        public AuthModeService(bool mockMode)
        { 
            _mockMode = mockMode;
        }

        public bool IsMockMode()
        {
            return _mockMode;   
        }
    }
}
