using BRaVe_Portal.Interfaces;

namespace BRaVe_Portal.Services
{
    public class AuthModeService : IAuthModeService
    {
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
