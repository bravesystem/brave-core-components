using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;

namespace BRaVe_Management_Backend.Interfaces.uat_login
{
    public interface IUatFormAccessService
    {   /// <summary>
        /// Registers a new UAT form user using an admin-provided token.
        /// Returns a populated auth result on success, or null on failure.
        /// </summary>
        Task<UatFormAuthResult?> RegisterAsync(
            string email,
            string displayName,
            string password,
            string adminToken,
            CancellationToken cancellationToken);

        /// <summary>
        /// Authenticates an existing UAT form user.
        /// Depending on your strategy, you can validate by password, token, or both.
        /// Returns a populated auth result on success, or null on failure.
        /// </summary>
        Task<UatFormAuthResult?> LoginAsync(
            string email,
            string? password,
            CancellationToken cancellationToken);

        /// <summary>
        /// Resets the password for an existing UAT form user.
        /// Intended for self-service / token-based flows.
        /// Returns true on success, false if the user or token is invalid.
        /// </summary>
        Task<bool> ResetPasswordAsync(
            string email,
            string newPassword,
            int tenantId,
            string adminToken,
            CancellationToken cancellationToken);

        /// <summary>
        /// Resets the password for an existing UAT form user directly by an administrator.
        /// No admin token is required; caller is assumed to be already authorized (e.g. via role).
        /// Returns true on success, false if the user cannot be found.
        /// </summary>
        Task<bool> ResetPasswordByAdminAsync(
            string email,
            string newPassword,
            int tenantId,
            CancellationToken cancellationToken);

        /// <summary>
        /// Activates a UAT form user account (sets IsActive = 1).
        /// Returns true on success, false if the user cannot be found.
        /// </summary>
        Task<bool> ActivateUserAsync(
            string email,
            int tenantId,
            CancellationToken cancellationToken);

        /// <summary>
        /// Deactivates a UAT form user account (sets IsActive = 0, without deleting).
        /// Returns true on success, false if the user cannot be found.
        /// </summary>
        Task<bool> DeactivateUserAsync(
            string email,
            int tenantId,
            CancellationToken cancellationToken);

        Task<TenantAndRolesDto> GetRolesAsync(string userId, 
            string languageCode = "en", CancellationToken ct = default);

    }
}
