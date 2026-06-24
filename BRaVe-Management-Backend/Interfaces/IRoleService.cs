using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.DTOs.RoleManagement;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IRoleService
    {
        Task<TenantAndRolesDto> GetRolesAsync(string email, string userDetails, string profileName, string languageCode="en", CancellationToken ct = default);
        Task AssignUserToTenant(string userId, int tenantId, string createdByUserId, CancellationToken ct = default);

        // Assign roles to a user for a tenant
        Task AssignUserRoles(string userId, int tenantId, string roleIds, string createdByUserId, CancellationToken ct = default);

        // Remove roles from a user for a tenant
        Task RemoveUserRoles(string userId, int tenantId, string roleIds, string deletedByUserId, CancellationToken ct = default);

        // Get all users with their roles and missions
        Task<IReadOnlyList<UserRoleInfo>> GetUserRolesAndMissionsAsync(string languageCode, int? tenantId = null, CancellationToken ct = default);


        //Create temporary access record
        Task<long> AddTemporaryAccess(TempAccessTableDto dto, CancellationToken ct = default);


        //Get temporary access record
        Task<List<TempAccessTableDto>> GetTemporaryAccess(CancellationToken ct = default);

        //Logout time for temporary access
        Task UpdateLogoutTimeForUserAsync(string userId, CancellationToken ct = default);

        //Effective user roles including Temp Table
        Task<List<UserRoleInfo>> GetEffectiveUserRolesInfoAsync(string userId, string languageCode, CancellationToken ct = default);

        //Get all system users
        Task<List<SystemUserDto>> GetSystemUsersAsync(int? tenantId, string languageCode, CancellationToken ct = default);




    }

}
