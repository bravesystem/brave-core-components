using BRaVe_Mobile_Backend.DTOs;

namespace BRaVe_Mobile_Backend.Interfaces
{
    public interface IRefreshTokenService
    {
        string generateToken();

        Task<RefreshTokenTenantPair> RefreshAsync(TokenRefreshDto req);
        
    }
}
