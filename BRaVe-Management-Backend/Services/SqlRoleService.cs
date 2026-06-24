
using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.DTOs.RoleManagement;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using System.Data;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;


namespace BRaVe_Management_Backend.Services
{
    public class SqlRoleService : IRoleService
    {
       
            private string connectionString { get; set; }
        private readonly ILogger<SqlProgramService> _logger;

        public SqlRoleService(ISecretProvider secretProvider,ILogger<SqlProgramService> logger)
        {
            connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection).Result;

            _logger = logger;

            _logger.LogInformation("SqlRoleService initialized with connection string from KeyVault");
        }

        //Get all users with roles and missions
        public async Task<IReadOnlyList<UserRoleInfo>> GetUserRolesAndMissionsAsync(
     string languageCode,
     int? tenantId = null,
     CancellationToken ct = default)
        {
            var flatResults = new List<(string ProfileName, string UserId, string Email,
                                        string MissionName, int RoleId, string RoleName)>();

            try
            {
                await using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync(ct);

                await using var cmd = new SqlCommand("sp_GetUserRolesAndMissions", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.Add("@Language", SqlDbType.NVarChar, 10).Value = languageCode;
                cmd.Parameters.Add("@TenantId", SqlDbType.Int).Value =
                    (object?)tenantId ?? DBNull.Value;

                await using var reader = await cmd.ExecuteReaderAsync(ct);

                while (await reader.ReadAsync(ct))
                {
                    flatResults.Add((
                        reader.IsDBNull(reader.GetOrdinal("ProfileName"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("ProfileName")),

                        reader.GetString(reader.GetOrdinal("UserId")),

                        reader.IsDBNull(reader.GetOrdinal("Email"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("Email")),

                        reader.GetString(reader.GetOrdinal("MissionName")),
                        reader.GetInt32(reader.GetOrdinal("RoleId")),
                        reader.GetString(reader.GetOrdinal("RoleName"))
                    ));
                }
            }
            catch (SqlException ex)
            {
                // SQL Server specific errors
                _logger.LogError(ex,
                    "SQL error executing sp_GetUserRolesAndMissions. TenantId: {TenantId}, Language: {Language}",
                    tenantId, languageCode);

                throw; // preserve stack trace
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("GetUserRolesAndMissionsAsync operation cancelled.");
                throw;
            }
            catch (Exception ex)
            {
                // Any unexpected error
                _logger.LogError(ex,
                    "Unexpected error in GetUserRolesAndMissionsAsync");

                throw;
            }

            // Group results
            var grouped = flatResults
                .GroupBy(x => new { x.ProfileName, x.UserId, x.Email, x.MissionName })
                .Select(g => new UserRoleInfo
                {
                    ProfileName = g.Key.ProfileName,
                    UserId = g.Key.UserId,
                    Email = g.Key.Email,
                    MissionName = g.Key.MissionName,
                    Roles = g.Select(r => r.RoleName).Distinct().ToList(),
                    RoleIds = g.Select(r => r.RoleId.ToString()).Distinct().ToList()
                })
                .ToList();

            return grouped.AsReadOnly();
        }




        // Get all role names for a user
        public async Task<TenantAndRolesDto> GetRolesAsync(string userId,string userDetails, string profileName, string languageCode, CancellationToken ct = default)
        {
            _logger.LogInformation("Fetching all roles for UserId={email}", userDetails);

            var tenantAndRolesDto = new TenantAndRolesDto();

                var roles = new List<int>();

           var userProfile = new UserRoleInfo();
            userProfile.UserId = userId;
            userProfile.Roles = new List<string>();
            userProfile.RoleIds = new List<string>();

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync(ct);

                // Call the stored procedure that checks TempMissionAccess Table first
                using var cmd = new SqlCommand("sp_GetUserRoles_TempFirst_V2", conn)
                { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.Add("@Language", SqlDbType.NVarChar, 10).Value = languageCode;
                cmd.Parameters.Add("@UserDetails", SqlDbType.NVarChar, 250).Value = userDetails;
                cmd.Parameters.Add("@ProfileName", SqlDbType.NVarChar, 250).Value = profileName;
                cmd.Parameters.Add("@CreatedByUserId", SqlDbType.VarChar, 50).Value = "SYSTEM";

                using var reader = await cmd.ExecuteReaderAsync(ct);

                _logger.LogInformation($"Successfully retrieved roles for user {userDetails}");

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

                    roles.Add( roleid );
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


        //Return logged in User Mission and role including temp mission access
        public async Task<List<UserRoleInfo>> GetEffectiveUserRolesInfoAsync(string userId, string languageCode, CancellationToken ct = default)
        {
            var list = new List<UserRoleInfo>();

            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync(ct);

            await using var cmd = new SqlCommand("sp_GetLoggedInUserRoles", conn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@UserId", SqlDbType.NVarChar, 256).Value = userId;
            cmd.Parameters.Add("@Language", SqlDbType.NVarChar, 10).Value = languageCode;

            await using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                list.Add(new UserRoleInfo
                {
                    UserId = reader.GetString(reader.GetOrdinal("UserId")),
                    ProfileName = "",  
                    MissionName = reader.GetString(reader.GetOrdinal("MissionName")),
                    Roles = new List<string>
            {
                reader.GetString(reader.GetOrdinal("RoleName"))
            },
                    RoleIds = new List<string>
            {
                reader.GetInt32(reader.GetOrdinal("RoleId")).ToString()
            }
                });
            }

            return list;
        }

        // Assign user to tenant (sp_AssignUserToTenant)
        public async Task AssignUserToTenant(string userId, int tenantId, string createdByUserId, CancellationToken ct = default)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync(ct);

            using var cmd = new SqlCommand("sp_AssignUserToTenant", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@TenantId", tenantId);
            cmd.Parameters.AddWithValue("@CreatedByUserId", createdByUserId);

            await cmd.ExecuteNonQueryAsync(ct);
        }

        // Assign user to roles (usp_AssignUserRoles)
        public async Task AssignUserRoles(string userId, int tenantId, string roleIds, string createdByUserId, CancellationToken ct = default)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync(ct);

            using var cmd = new SqlCommand("sp_AssignUserRoles", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@TenantId", tenantId);
            cmd.Parameters.AddWithValue("@RoleIds", roleIds); // comma-separated
            cmd.Parameters.AddWithValue("@CreatedByUserId", createdByUserId);

            await cmd.ExecuteNonQueryAsync(ct);
        }

        // Remove user roles (sp_RemoveUserRoles)
        public async Task RemoveUserRoles(string userId, int tenantId, string roleIds, string deletedByUserId, CancellationToken ct = default)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync(ct);

            using var cmd = new SqlCommand("sp_RemoveUserRoles", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@TenantId", tenantId);
            cmd.Parameters.AddWithValue("@RoleIds", roleIds); // comma-separated
            cmd.Parameters.AddWithValue("@DeletedByUserId", deletedByUserId);

            await cmd.ExecuteNonQueryAsync(ct);
        }

        //Add temporary mission access request 
        public async Task<long> AddTemporaryAccess(TempAccessTableDto dto, CancellationToken ct = default)
        {
            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync(ct);

            // Insert the values passed into Temp Mission Access Table
            var sql = @"
        INSERT INTO dbo.tbl_TempMissionAccess (UserId, MissionId, RoleId, Reason)
        VALUES (@UserId, @MissionId, @RoleId, @Reason);

        SELECT CAST(SCOPE_IDENTITY() AS BIGINT);
    ";

            await using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add("@UserId", SqlDbType.NVarChar, 256).Value = dto.UserId;
            cmd.Parameters.Add("@MissionId", SqlDbType.Int).Value = dto.MissionId;
            cmd.Parameters.Add("@RoleId", SqlDbType.Int).Value = dto.RoleId;
            cmd.Parameters.Add("@Reason", SqlDbType.NVarChar, 500).Value = (object?)dto.Reason ?? DBNull.Value;

            var result = await cmd.ExecuteScalarAsync(ct);

            return Convert.ToInt64(result);
        }


        //Update Log out time for Temp Access

        public async Task UpdateLogoutTimeForUserAsync(string userId, CancellationToken ct = default)
        {
            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync(ct);

            var sql = @"
        UPDATE dbo.tbl_TempMissionAccess
        SET LogoutTime = SYSDATETIMEOFFSET()
        WHERE UserId = @UserId
          AND AccessedAt IS NOT NULL
          AND LogoutTime IS NULL;
    ";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@UserId", SqlDbType.NVarChar, 256).Value = userId;
            await cmd.ExecuteNonQueryAsync(ct);
        }




        // Get values from Temp Mission Access Table
        public async Task<List<TempAccessTableDto>> GetTemporaryAccess(CancellationToken ct = default)
        {
            var items = new List<TempAccessTableDto>();

            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync(ct);

            // Step 1: Mark records older than 1 hour as consumed
            var updateSql = @"
        UPDATE dbo.tbl_TempMissionAccess
        SET IsConsumed = 1
        WHERE IsConsumed = 0
          AND DATEADD(HOUR, 1, RequestedAt) <= SYSDATETIMEOFFSET();
    ";

            await using (var updateCmd = new SqlCommand(updateSql, conn))
            {
                await updateCmd.ExecuteNonQueryAsync(ct);
            }

            // Step 2: Fetch all records
            var selectSql = @"
         SELECT case when UserDetails is null then t.UserId else UserDetails end UserId, 
            MissionId, RoleId, RequestedAt, AccessedAt, Reason, IsConsumed,LogoutTime
        FROM tbl_TempMissionAccess t 
        join tbl_UserMissionRequests m on t.UserId=m.UserId
         ORDER BY RequestedAt DESC;";

            await using var cmd = new SqlCommand(selectSql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                var dto = new TempAccessTableDto
                {
                    UserId = reader["UserId"].ToString()!,
                    MissionId = reader.GetInt32(reader.GetOrdinal("MissionId")),
                    RoleId = reader.GetInt32(reader.GetOrdinal("RoleId")),

                    RequestedAt = reader["RequestedAt"] == DBNull.Value
                        ? null
                        : (DateTimeOffset?)reader.GetDateTimeOffset(reader.GetOrdinal("RequestedAt")),

                    AccessedAt = reader["AccessedAt"] == DBNull.Value
                        ? null
                        : (DateTimeOffset?)reader.GetDateTimeOffset(reader.GetOrdinal("AccessedAt")),

                    LogoutTime = reader["LogoutTime"] == DBNull.Value
                        ? null
                        : (DateTimeOffset?)reader.GetDateTimeOffset(reader.GetOrdinal("LogoutTime")),

                    Reason = reader["Reason"].ToString(),
                    IsConsumed = reader.GetBoolean(reader.GetOrdinal("IsConsumed"))
                };

                items.Add(dto);
            }

            return items;
        }

        /// <summary>
        /// Returns all system users that have been processed
        /// </summary>

        public async Task<List<SystemUserDto>> GetSystemUsersAsync(int? tenantId, string languageCode, CancellationToken ct = default)
        {
            var flat = new List<(string UserId, string ProfileName, string? MissionName, int? RoleId, string? RoleName)>();

            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync(ct);

            var sql = "sp_GetSystemUsers";   

            await using var cmd = new SqlCommand(sql, conn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@TenantId", SqlDbType.Int).Value = (object?)tenantId ?? DBNull.Value;
            cmd.Parameters.Add("@Language", SqlDbType.NVarChar, 10).Value = languageCode ?? "en";

            await using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                var userId = reader.IsDBNull(reader.GetOrdinal("UserId"))
                    ? string.Empty
                    : reader.GetString(reader.GetOrdinal("UserId"));

                var profile = reader.IsDBNull(reader.GetOrdinal("ProfileName"))
                    ? string.Empty
                    : reader.GetString(reader.GetOrdinal("ProfileName"));

                string? missionName = reader.IsDBNull(reader.GetOrdinal("MissionName"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("MissionName"));

                int? roleId = reader.IsDBNull(reader.GetOrdinal("RoleId"))
                    ? null
                    : reader.GetInt32(reader.GetOrdinal("RoleId"));

                string? roleName = reader.IsDBNull(reader.GetOrdinal("RoleName"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("RoleName"));

                flat.Add((userId, profile, missionName, roleId, roleName));
            }

            var grouped = flat
                .GroupBy(x => new { x.UserId, x.ProfileName, x.MissionName })
                .Select(g => new SystemUserDto
                {
                    UserId = g.Key.UserId,
                    ProfileName = g.Key.ProfileName,
                    MissionName = g.Key.MissionName,
                    RoleIds = g.Select(r => r.RoleId)
                               .Where(rid => rid.HasValue)
                               .Select(rid => rid!.Value)
                               .Distinct()
                               .ToList(),
                    Roles = g.Select(r => r.RoleName)
                             .Where(rn => !string.IsNullOrWhiteSpace(rn))
                             .Distinct()
                             .ToList()
                })
                .ToList();

            return grouped;
        }


    }
}