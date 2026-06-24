using BRaVe_Portal.Models.DTOs;

namespace BRaVe_Portal.Interfaces
{
    public interface IAccessControlClient
    {
        Task<TenantAndRolesDto> GetRolesAsync(string userId,string language, CancellationToken ct = default);
        Task UpdateLogoutTimeAsync(CancellationToken ct = default);
        Task FormRegisterUser(UatFormRegisterDto dto, CancellationToken ct = default);
        Task<TenantAndRolesDto> GetFormAuthRolesAsync(string userId, string language, CancellationToken ct = default);
    }
}
