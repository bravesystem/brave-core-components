using BRaVe_Mobile_Backend.Models;
using System.Security.Claims;

namespace BRaVe_Mobile_Backend.Interfaces
{
    public interface IRequestContextValidator
    {
        DeviceProfile DeviceProfile();
        Task<ValidationResult> ValidateAsync(ClaimsPrincipal user, string? requestIp);
    }
}
