using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.DTOs.RoleManagement;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Interfaces.uat_login;
using BRaVe_Management_Backend.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace BRaVe_Management_Backend.Services
{
    public class SqlUatFormAccessService : IUatFormAccessService
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlUatFormAccessService> _logger;

        public SqlUatFormAccessService( ISecretProvider secretProvider, ILogger<SqlUatFormAccessService> logger)
        {
            // Synchronous wait is acceptable here, same pattern as BearerTokenHandler
            _connectionString = secretProvider
                .GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection)
                .Result;

            _logger = logger;
        }

        public async Task<UatFormAuthResult?> RegisterAsync( string email, string displayName, string password, string adminToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(adminToken))
            {
                return null;
            }

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            try
            {
                await using var cmd = new SqlCommand("dbo.sp_RegisterUatFormAccessUser", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@DisplayName", displayName);
                cmd.Parameters.AddWithValue("@Password", password);
                cmd.Parameters.AddWithValue("@TokenValue", adminToken);

                await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                var userId = reader.GetGuid(reader.GetOrdinal("UserId"));
                var tenantId = reader.GetInt32(reader.GetOrdinal("TenantId"));

                return new UatFormAuthResult
                {
                    UserId = userId,
                    Email = email,
                    DisplayName = displayName,
                    TenantId = tenantId,
                    // Admin token is used only for onboarding; do not surface it back.
                    Token = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during UAT form registration for {Email}", email);
                return null;
            }
        }

        public async Task<UatFormAuthResult?> LoginAsync(string email,string password, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            const string sql = @"
                    SELECT TOP 1 Id,TenantId, Email, DisplayName, PasswordHash, Token
                    FROM dbo.tbl_UatFormUsers
                    WHERE Email = @Email
                    AND IsActive = 1;";

            Guid userId = Guid.Empty;
            int tenantId = 0;
            string? dbEmail = null;
            string? displayName = null;
            byte[]? passwordHash = null;

            await using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Email", email);

                await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                userId = reader.GetGuid(reader.GetOrdinal("Id"));
                tenantId = reader.GetInt32(reader.GetOrdinal("TenantId"));
                dbEmail = reader.GetString(reader.GetOrdinal("Email"));
                displayName = reader.GetString(reader.GetOrdinal("DisplayName"));
                passwordHash = (byte[])reader["PasswordHash"];
            }

            // Basic validation strategy:
            // - If a password hash exists, require a matching password.
            // - Optionally, if a token exists, require it to match when provided.
            if (passwordHash != null)
            {
                if (string.IsNullOrEmpty(password) ||
                    !VerifyPassword(password, passwordHash))
                {
                    _logger.LogInformation($"Provide password for user {email} cannot be verified");
                    return null;
                }
            }

            return new UatFormAuthResult
            {
                UserId = userId,
                Email = dbEmail ?? email,
                DisplayName = displayName ?? string.Empty,
                TenantId = tenantId
            };
        }

        public async Task<bool> ResetPasswordAsync(  //ok
            string email,
            string newPassword,
            int tenantId,
            string adminToken,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(adminToken))
            {
                return false;
            }

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);
            try
            {
                await using var cmd = new SqlCommand("dbo.sp_ResetPassword", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@NewPassword", newPassword);
                cmd.Parameters.AddWithValue("@TenantId", tenantId);
                cmd.Parameters.AddWithValue("@TokenValue", adminToken);

                await cmd.ExecuteNonQueryAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password (token-based) for {Email}", email);
                return false;
            }
        }

        public async Task<bool> ResetPasswordByAdminAsync(
            string email,
            string newPassword,
            int tenantId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(newPassword))
            {
                return false;
            }

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            try
            {
                await using var cmd = new SqlCommand("dbo.sp_ResetPasswordAdmin", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@NewPassword", newPassword);
                cmd.Parameters.AddWithValue("@TenantId", tenantId);

                await cmd.ExecuteNonQueryAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password by admin for {Email}", email);
                return false;
            }
        }

        public async Task<bool> ActivateUserAsync(
            string email,
            int tenantId,
            CancellationToken cancellationToken)
        {
            return await SetIsActiveAsync(email, tenantId, true, cancellationToken);
        }

        public async Task<bool> DeactivateUserAsync(
            string email,
            int tenantId,
            CancellationToken cancellationToken)
        {
            return await SetIsActiveAsync(email, tenantId, false, cancellationToken);
        }

        private async Task<bool> SetIsActiveAsync(string email,int tenantId,bool isActive,CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            const string sql = @"
                UPDATE dbo.tbl_UatFormUsers
                SET IsActive = @IsActive
                WHERE TenantId = @TenantId AND Email = @Email;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@IsActive", isActive);
            cmd.Parameters.AddWithValue("@TenantId", tenantId);
            cmd.Parameters.AddWithValue("@Email", email);

            var rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
            return rows > 0;
        }

        private static byte[] HashPassword(string password)
        {

            // SQL NVARCHAR uses UTF-16 LE (little endian)
            byte[] bytes = password is null ? Encoding.Unicode.GetBytes(string.Empty)
                                         : Encoding.Unicode.GetBytes(password);

            using var sha = SHA256.Create();
            return sha.ComputeHash(bytes); // 32 bytes, same as HASHBYTES('SHA2_256', ...)

        }

        private static bool VerifyPassword(string password, byte[] hash)
        {
            var computed = HashPassword(password);
            if (computed.Length != hash.Length)
            {
                return false;
            }

            var diff = 0;
            for (var i = 0; i < computed.Length; i++)
            {
                diff |= computed[i] ^ hash[i];
            }

            return diff == 0;
        }

        public async Task<TenantAndRolesDto> GetRolesAsync(string userId, string languageCode = "en", CancellationToken ct = default)
        {
            _logger.LogInformation("Fetching all roles for UserId={userId}", userId);

            var tenantAndRolesDto = new TenantAndRolesDto();

            var roles = new List<int>();

            var userProfile = new UserRoleInfo();
            userProfile.UserId = userId;
            userProfile.Roles = new List<string>();
            userProfile.RoleIds = new List<string>();

            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync(ct);

                // Call the stored procedure that checks TempMissionAccess Table first
                using var cmd = new SqlCommand("sp_GetFormUserRoles", conn)
                { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.Add("@Language", SqlDbType.NVarChar, 10).Value = languageCode;
                
                using var reader = await cmd.ExecuteReaderAsync(ct);

                _logger.LogInformation($"Successfully retrieved roles for user {userId}");

                while (await reader.ReadAsync(ct))
                {
                    if (tenantAndRolesDto.TenantId == 0)
                    {
                        tenantAndRolesDto.TenantId =
                            reader.IsDBNull(reader.GetOrdinal("TenantId")) ? 0 :
                            reader.GetInt32(reader.GetOrdinal("TenantId"));
                    }

                    if (string.IsNullOrEmpty(userProfile.MissionName))
                    {
                        userProfile.MissionName =
                            reader.IsDBNull(reader.GetOrdinal("MissionName")) ? "" :
                            reader.GetString(reader.GetOrdinal("MissionName"));

                    }

                    int roleid = reader.GetInt32(reader.GetOrdinal("RoleId"));

                    roles.Add(roleid);
                    userProfile.RoleIds.Add(roleid.ToString());
                    userProfile.Roles.Add(
                        reader.GetString(reader.GetOrdinal("RoleName"))
                    );
                }


                tenantAndRolesDto.Roles = roles.AsReadOnly();
                tenantAndRolesDto.UserProfile = userProfile;


                return tenantAndRolesDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection failed");
                throw ex;
            }

        }
    }

}
