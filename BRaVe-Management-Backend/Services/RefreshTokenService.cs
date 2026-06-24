using BRaVe_Management_Backend.Interfaces;
using System.Security.Cryptography;

namespace BRaVe_Management_Backend.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        public string GenerateToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }
    }
}