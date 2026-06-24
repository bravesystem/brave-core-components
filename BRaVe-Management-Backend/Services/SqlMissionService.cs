using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using DocumentFormat.OpenXml.EMMA;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BRaVe_Management_Backend.Services
{
    public class SqlMissionService : IMissionService
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlMissionService> _logger;

        public SqlMissionService(ISecretProvider secretProvider, ILogger<SqlMissionService> logger)
        {
            _logger = logger;
            _connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection).Result;
            _logger.LogInformation("SqlMissionService initialized with connection string from KeyVault.");
        }

        public async Task<string> CreateMission(MissionDto data, string userId)
        {
            _logger.LogInformation("Creating mission for CountryIso2={CountryIso2}, Name={Name}, CreatedBy={UserId}", data.CountryIso2, data.Name, userId);

            using var conn = new SqlConnection(_connectionString);
            var cmd = new SqlCommand(@"sp_CreateMission @CountryIso2, @Name, @FocalpointType, @Note, @CreatedByUserId, @OutTenantCode out", conn);

            cmd.Parameters.AddWithValue("@CountryIso2", data.CountryIso2);
            cmd.Parameters.AddWithValue("@Name", data.Name);
            cmd.Parameters.AddWithValue("@FocalpointType", data.FocalpointType);
            cmd.Parameters.AddWithValue("@Note", data.Note);
            cmd.Parameters.AddWithValue("@CreatedByUserId", userId);

            var outCodeParam = new SqlParameter("@OutTenantCode", SqlDbType.VarChar, 10)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(outCodeParam);

            try
            {
                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                var tenantCode = outCodeParam.Value as string;
                _logger.LogInformation("Successfully created mission '{MissionName}' with TenantCode={TenantCode}", data.Name, tenantCode);

                return tenantCode;
            }
            catch (SqlException ex)
            {
                if (ex.Number == 50010 || ex.Message.Contains("DUPLICATE_CODE", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Duplicate mission detected for Name={MissionName}, CountryIso2={CountryIso2}", data.Name, data.CountryIso2);
                    return null;
                }

                _logger.LogError(ex, "SQL error while creating mission '{MissionName}'", data.Name);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating mission '{MissionName}'", data.Name);
                throw;
            }
        }

        public async Task DeleteMission(int id)
        {
            _logger.LogInformation("Attempting to delete mission with ID={MissionId}", id);

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"DELETE m 
                                       FROM tbl_Missions m
                                       LEFT JOIN tbl_RiskBenefitAssessments a on a.TenantId = m.MissionId
                                       WHERE m.MissionId = @MissionId AND a.AssessmentId IS NULL;", conn);
            cmd.Parameters.AddWithValue("@MissionId", id);

            try
            {
                var rows = await cmd.ExecuteNonQueryAsync();
                if (rows > 0)
                {
                    _logger.LogInformation("Successfully deleted mission with ID={MissionId}", id);
                }
                else
                {
                    _logger.LogWarning("No mission deleted. Mission ID={MissionId} may not exist or has linked assessments.", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting mission with ID={MissionId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Mission>> GetAllMissions()
        {
            _logger.LogInformation("Fetching all missions from database...");
            var missions = new List<Mission>();

            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                const string sql = "SELECT MissionId, CountryIso2, TenantCode, [Name], FocalpointType, Note FROM tbl_Missions";
                using var cmd = new SqlCommand(sql, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    missions.Add(new Mission
                    {
                        MissionId = reader.GetInt32(reader.GetOrdinal("MissionId")),
                        CountryIso2 = reader.GetString(reader.GetOrdinal("CountryIso2")),
                        TenantCode = reader.GetString(reader.GetOrdinal("TenantCode")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        FocalpointType = reader.GetInt32(reader.GetOrdinal("FocalpointType")),
                        Note = reader.GetString(reader.GetOrdinal("Note"))
                    });
                }

                _logger.LogInformation("Retrieved {Count} missions from database.", missions.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all missions from database.");
            }

            return missions;
        }

        public async Task<Mission?> GetMission(int id)
        {
            _logger.LogInformation("Fetching mission with ID={MissionId}", id);

            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                const string sql = "SELECT MissionId, CountryIso2, TenantCode, Name, FocalpointType, Note FROM tbl_Missions WHERE MissionId=@MissionId";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@MissionId", id);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var mission = new Mission
                    {
                        MissionId = reader.GetInt32(reader.GetOrdinal("MissionId")),
                        CountryIso2 = reader.GetString(reader.GetOrdinal("CountryIso2")),
                        TenantCode = reader.GetString(reader.GetOrdinal("TenantCode")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        FocalpointType = reader.GetInt32(reader.GetOrdinal("FocalpointType")),
                        Note = reader.GetString(reader.GetOrdinal("Note"))
                    };

                    _logger.LogInformation("Successfully retrieved mission '{MissionName}' (ID={MissionId})", mission.Name, id);
                    return mission;
                }

                _logger.LogWarning("Mission with ID={MissionId} not found.", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving mission with ID={MissionId}", id);
            }

            return null;
        }

        public async Task UpdateMission(Mission data, string userId)
        {
            _logger.LogInformation("Updating mission ID={MissionId}, Name={Name}, UpdatedBy={UserId}", data.MissionId, data.Name, userId);

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"UPDATE tbl_Missions 
                                       SET Name=@Name, Note=@Note, FocalpointType=@FocalpointType, 
                                           UpdatedByUserId=@UpdatedByUserId, UpdatedOn=GETUTCDATE() 
                                       WHERE MissionId=@MissionId", conn);

            cmd.Parameters.AddWithValue("@MissionId", data.MissionId);
            cmd.Parameters.AddWithValue("@Name", data.Name);
            cmd.Parameters.AddWithValue("@FocalpointType", data.FocalpointType);
            cmd.Parameters.Add("@Note", SqlDbType.NVarChar).Value =
            (object?)data.Note ?? DBNull.Value;
            cmd.Parameters.AddWithValue("@UpdatedByUserId", userId);

            try
            {
                var rows = await cmd.ExecuteNonQueryAsync();
                if (rows > 0)
                {
                    _logger.LogInformation("Mission ID={MissionId} updated successfully by {UserId}", data.MissionId, userId);
                }
                else
                {
                    _logger.LogWarning("Mission ID={MissionId} not found for update.", data.MissionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating mission ID={MissionId}", data.MissionId);
                throw;
            }
        }
        public async Task<IEnumerable<Mission>> GetAllMissionsByCountry(string id)
        {
            _logger.LogInformation("Fetching all missions for CountryIso2={CountryIso2}", id);

            var missions = new List<Mission>();

            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                const string sql = @"
            SELECT 
                MissionId,
                CountryIso2,
                TenantCode,
                Name,
                FocalpointType,
                Note
            FROM tbl_Missions 
            WHERE CountryIso2 = @CountryIso2";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@CountryIso2", id);

                using var reader = await cmd.ExecuteReaderAsync();

                var ordMissionId = reader.GetOrdinal("MissionId");
                var ordCountryIso2 = reader.GetOrdinal("CountryIso2");
                var ordTenantCode = reader.GetOrdinal("TenantCode");
                var ordName = reader.GetOrdinal("Name");
                var ordFocalpointType = reader.GetOrdinal("FocalpointType");
                var ordNote = reader.GetOrdinal("Note");

                while (await reader.ReadAsync())
                {
                    missions.Add(new Mission
                    {
                        MissionId = reader.GetInt32(ordMissionId),
                        CountryIso2 = reader.GetString(ordCountryIso2),
                        TenantCode = reader.GetString(ordTenantCode),
                        Name = reader.GetString(ordName),
                        FocalpointType = reader.GetInt32(ordFocalpointType),

                        Note = reader.IsDBNull(ordNote)
                            ? null
                            : reader.GetString(ordNote)
                    });
                }

                _logger.LogInformation(
                    "Retrieved {Count} missions for CountryIso2={CountryIso2}",
                    missions.Count,
                    id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving missions for CountryIso2={CountryIso2}", id);
                throw;
            }

            return missions;
        }

        //Get mission for a specific user
        public async Task<Mission?> GetUserMission(string userId)
        {
            _logger.LogInformation("Fetching mission for UserId={UserId}", userId);

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                const string sql = @"
            SELECT ura.UserId,m.missionId,m.CountryIso2, m.Name,m.tenantcode
            FROM tbl_UserRoleAssignments ura
            INNER JOIN tbl_Missions m ON m.MissionId = ura.TenantId
            WHERE ura.UserId = @UserId";

                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserId", userId);

                await using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var mission = new Mission
                    {
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        MissionId = reader.GetInt32(reader.GetOrdinal("MissionId")),
                        TenantCode = reader.GetString(reader.GetOrdinal("TenantCode")),
                        CountryIso2 = reader.GetString(reader.GetOrdinal("CountryIso2"))
                    };

                    _logger.LogInformation(
                        "Successfully retrieved mission '{MissionName}' for UserId={UserId}",
                        mission.Name, userId);

                    return mission;
                }

                _logger.LogWarning("No mission found for UserId={UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving mission for UserId={UserId}", userId);
            }

            return null;
        }

        public async Task BootstrapMission(string tenantCode)
        {
            _logger.LogInformation("Initialize mission context for Mission: {tenantCode}", tenantCode);

            using var conn = new SqlConnection(_connectionString);
            var cmd = new SqlCommand(@"sp_BootstrapMission @TenantCode", conn);

            cmd.Parameters.AddWithValue("@TenantCode", tenantCode);
           
            try
            {
                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                _logger.LogInformation("Successfully Initialize all mission setup for mission: '{MissionName}'", tenantCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while initializing mission setup for mission: '{MissionName}'", tenantCode);
            }
        }
    
    }
}
