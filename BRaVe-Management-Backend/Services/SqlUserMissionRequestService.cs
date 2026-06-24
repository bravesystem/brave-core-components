using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BRaVe_Management_Backend.Services
{
    public class SqlUserMissionRequestService : IUserMissionMappingService
    {
        private readonly string connectionString;
        private readonly ILogger<SqlUserMissionRequestService> _logger;

        public SqlUserMissionRequestService(ISecretProvider secretProvider, ILogger<SqlUserMissionRequestService> logger)
        {
            _logger = logger;
            connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection).Result;
        }

        public async Task CreateUserRequest(UserRequestDto data)
        {
            _logger.LogInformation("Creating user request for UserId {UserId}", data.UserId);

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            var sql = @"
                INSERT INTO tbl_UserMissionRequests 
                (UserId, ProfileName, UserDetails, Justification, RequestedOn, IsProcessed, ProcessedByUserId)
                VALUES 
                (@UserId, @ProfileName, @UserDetails, @Justification, GETDATE(), 0, @ProcessedByUserId)";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@UserId", data.UserId);
            cmd.Parameters.AddWithValue("@ProfileName", data.ProfileName);
            cmd.Parameters.AddWithValue("@UserDetails", (object?)data.UserDetails ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Justification", data.Justification);
            cmd.Parameters.AddWithValue("@ProcessedByUserId", "SYSTEM");

            try
            {
                await cmd.ExecuteNonQueryAsync();
                _logger.LogInformation("User request created successfully for UserId {UserId}", data.UserId);
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627 || ex.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Duplicate user request attempt for UserId {UserId}", data.UserId);
                    throw new Exception("A request for this user already exists.");
                }

                _logger.LogError(ex, "SQL error while creating user request for UserId {UserId}", data.UserId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating user request for UserId {UserId}", data.UserId);
                throw;
            }
        }

        public async Task<IEnumerable<UserMissionRequest>> GetAllUserRequests()
        {
            _logger.LogInformation("Fetching all unprocessed user mission requests");

            var requests = new List<UserMissionRequest>();

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = "SELECT * FROM tbl_UserMissionRequests WHERE IsProcessed=0";
                using var cmd = new SqlCommand(sql, conn);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    requests.Add(new UserMissionRequest
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        UserId = reader.GetString(reader.GetOrdinal("UserId")),
                        ProfileName = reader.GetString(reader.GetOrdinal("ProfileName")),
                        UserDetails = reader.IsDBNull(reader.GetOrdinal("UserDetails")) ? null : reader.GetString(reader.GetOrdinal("UserDetails")),
                        Justification = reader.GetString(reader.GetOrdinal("Justification")),
                        RequestedOn = reader.GetDateTime(reader.GetOrdinal("RequestedOn")),
                        IsProcessed = reader.GetBoolean(reader.GetOrdinal("IsProcessed")),
                        ProcessedByUserId = reader.GetString(reader.GetOrdinal("ProcessedByUserId")),
                        ProcessededOn = reader.IsDBNull(reader.GetOrdinal("ProcessededOn")) ? null : reader.GetDateTime(reader.GetOrdinal("ProcessededOn"))
                    });
                }

                _logger.LogInformation("Fetched {Count} unprocessed user mission requests", requests.Count);
                return requests;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching unprocessed user mission requests");
                throw;
            }
        }

        public async Task<UserMissionRequest?> GetUserRequestById(string userId)
        {
            _logger.LogInformation("Fetching user mission request for UserId {UserId}", userId);

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = "SELECT * FROM tbl_UserMissionRequests WHERE UserId = @UserId";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserId", userId);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var request = new UserMissionRequest
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        UserId = reader.GetString(reader.GetOrdinal("UserId")),
                        ProfileName = reader.GetString(reader.GetOrdinal("ProfileName")),
                        UserDetails = reader.IsDBNull(reader.GetOrdinal("UserDetails")) ? null : reader.GetString(reader.GetOrdinal("UserDetails")),
                        Justification = reader.GetString(reader.GetOrdinal("Justification")),
                        RequestedOn = reader.GetDateTime(reader.GetOrdinal("RequestedOn")),
                        IsProcessed = reader.GetBoolean(reader.GetOrdinal("IsProcessed")),
                        ProcessedByUserId = reader.GetString(reader.GetOrdinal("ProcessedByUserId")),
                        ProcessededOn = reader.IsDBNull(reader.GetOrdinal("ProcessededOn")) ? null : reader.GetDateTime(reader.GetOrdinal("ProcessededOn"))
                    };

                    _logger.LogInformation("User request found for UserId {UserId}", userId);
                    return request;
                }

                _logger.LogInformation("No user request found for UserId {UserId}", userId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user mission request for UserId {UserId}", userId);
                throw;
            }
        }

        public async Task AssignTenantToUser(UserMissionResultDto userMission, string UpdatedBy)
        {
            _logger.LogInformation("Assigning tenant {TenantId} and role {RoleId} to request {RequestId} by {UpdatedBy}", userMission.MissionId, userMission.RoleId, userMission.RequestId, UpdatedBy);

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                string? userId = null;
                using (var getUserCmd = new SqlCommand("SELECT UserId FROM tbl_UserMissionRequests WHERE Id = @RequestId", conn))
                {
                    getUserCmd.Parameters.AddWithValue("@RequestId", userMission.RequestId);
                    var result = await getUserCmd.ExecuteScalarAsync();
                    userId = result as string;
                }

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User request not found for RequestId {RequestId}", userMission.RequestId);
                    throw new Exception("User request not found.");
                }

                using var cmd = new SqlCommand("sp_AssignTenantToUser", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@TenantId", userMission.MissionId);
                cmd.Parameters.AddWithValue("@RoleId", userMission.RoleId);
                cmd.Parameters.AddWithValue("@CreatedByUserId", UpdatedBy);

                await cmd.ExecuteNonQueryAsync();
                _logger.LogInformation("Tenant assigned successfully to UserId {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning tenant and role to request {RequestId}", userMission.RequestId);
                throw;
            }
        }

        public async Task<bool> GetIfUserExists(string UserId)
        {
            _logger.LogInformation("Checking if user request exists for UserId {UserId}", UserId);

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = "SELECT 1 FROM tbl_UserMissionRequests WHERE UserId = @UserId";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserId", UserId);

                using var reader = await cmd.ExecuteReaderAsync();
                bool exists = await reader.ReadAsync();

                _logger.LogInformation("User request existence check for UserId {UserId}: {Exists}", UserId, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user request exists for UserId {UserId}", UserId);
                throw;
            }
        }


        //Direct user mission assignement

        public async Task CreateMissionPreAssignment(MissionPreAssignmentDto data)
        {
            _logger.LogInformation(
                "Creating mission pre-assignment for user {UserDetails}, TenantId={TenantId}, RoleId={RoleId}",
                data.UserDetails, data.TenantId, data.RoleId);

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = @"
            INSERT INTO tbl_MissionPreAssignedUsers
            (UserDetails, TenantId, RoleId)
            VALUES
            (@UserDetails, @TenantId, @RoleId)";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserDetails", data.UserDetails);
                cmd.Parameters.AddWithValue("@TenantId", data.TenantId);
                cmd.Parameters.AddWithValue("@RoleId", data.RoleId);

                await cmd.ExecuteNonQueryAsync();

                _logger.LogInformation(
                    "Mission pre-assignment created for user {UserDetails}",
                    data.UserDetails);
            }
            catch (SqlException ex)
            {
                // PK violation (email already exists)
                if (ex.Number == 2627 || ex.Message.Contains("PRIMARY", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "Duplicate pre-assignment attempt for user {UserDetails}",
                        data.UserDetails);

                    throw new Exception("User is already pre-assigned to a mission.");
                }

                _logger.LogError(
                    ex,
                    "SQL error creating mission pre-assignment for user {UserDetails}",
                    data.UserDetails);

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error creating mission pre-assignment for user {UserDetails}",
                    data.UserDetails);

                throw;
            }
        }




        public async Task<List<string>> CreateMissionBulkPreAssignment(List<MissionPreAssignmentDto> data)
        {
       
            try
            {
                using var conn = new SqlConnection(connectionString);
                using var cmd = new SqlCommand(
                    "dbo.sp_CreateBulkPreAssignUsers", conn);

                cmd.CommandType = CommandType.StoredProcedure;

                //Build TVP
                var table = new DataTable();
                table.Columns.Add("UserDetails", typeof(string));
                table.Columns.Add("TenantId", typeof(int));
                table.Columns.Add("RoleId", typeof(int));
                table.Columns.Add("CreatedBy", typeof(string));

                foreach (var item in data)
                {
                    table.Rows.Add(
                        item.UserDetails,
                        item.TenantId,
                        item.RoleId,
                        item.CreatedBy
                    );
                }

                //  Add TVP parameter
                var param = cmd.Parameters.AddWithValue(
                    "@Assignments", table);

                param.SqlDbType = SqlDbType.Structured;
                param.TypeName = "dbo.MissionPreAssignmentTvp";

                await conn.OpenAsync();

                // Read failed (existing) users
                var failedUsers = new List<string>();

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    failedUsers.Add(reader.GetString(0));
                }

                return failedUsers;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error during bulk mission pre-assignment");
                throw;
            }
        }

        public async Task<IEnumerable<PreAssignedUserDto>> GetPendingPreAssignedUsers(int tenantId)
        {
            _logger.LogInformation(
                "Fetching pending pre-assigned users (TenantId={TenantId})", tenantId);

            var results = new List<PreAssignedUserDto>();

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = @"
            SELECT 
                mpa.UserDetails,
                m.Name AS Mission,
                r.Text AS Role,
                mpa.CreatedBy,
                mpa.CreatedOn
            FROM dbo.tbl_MissionPreAssignedUsers mpa
            INNER JOIN tbl_missions m
                ON m.MissionId = mpa.TenantId
            INNER JOIN lkp_Role_Translations r
                ON r.RoleId = mpa.RoleId
            WHERE mpa.IsAccessed = 0
              AND r.LanguageCode = 'en'
              AND (@TenantId = 0 OR mpa.TenantId = @TenantId)
            ORDER BY mpa.CreatedOn DESC";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@TenantId", tenantId);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(new PreAssignedUserDto
                    {
                        UserDetails = reader.GetString(reader.GetOrdinal("UserDetails")),
                        MissionName = reader.GetString(reader.GetOrdinal("Mission")),
                        RoleName = reader.GetString(reader.GetOrdinal("Role")),
                        CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("CreatedBy")),

                        CreatedOn = reader.IsDBNull(reader.GetOrdinal("CreatedOn"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("CreatedOn"))
                    });
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pending pre-assigned users");
                throw;
            }
        }




    }
}
