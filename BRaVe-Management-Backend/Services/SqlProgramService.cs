using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using BRaVe_Portal.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text.Json;

namespace BRaVe_Management_Backend.Services
{
    public class SqlProgramService : IProgramService
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlProgramService> _logger;

        public SqlProgramService(ISecretProvider secretProvider, ILogger<SqlProgramService> logger)
        {
            _connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection).Result;
            _logger = logger;

            _logger.LogInformation("SqlProgramService initialized with connection string from KeyVault");
        }

        public async Task<IEnumerable<ProgramDef>> GetAllProgramsByTenant(string UserId, int TenantId)
        {
            _logger.LogInformation("Fetching all programs for TenantId={TenantId}", TenantId);

            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                _logger.LogDebug("Database connection opened successfully for TenantId={TenantId}", TenantId);

                var programDefs = new List<ProgramDef>();

                using (var cmd = new SqlCommand("sp_GetAllPrograms", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@TenantId", TenantId);
                    cmd.Parameters.AddWithValue("@UserId", UserId);

                    using var reader = await cmd.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        var programDef = new ProgramDef
                        {
                            ProgramId = reader.GetInt32(reader.GetOrdinal("ProgramId")),
                            ProgramCode = reader.GetString(reader.GetOrdinal("ProgramCode")),
                            AssessmentId = reader.IsDBNull(reader.GetOrdinal("AssessmentId")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("AssessmentId")),
                            TenantId = TenantId,
                            Title = reader.IsDBNull(reader.GetOrdinal("Title")) ? null : reader.GetString(reader.GetOrdinal("Title")),
                            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                            CreatedOn = reader.GetDateTime(reader.GetOrdinal("CreatedOn")),
                            UpdatedByUserId = reader.IsDBNull(reader.GetOrdinal("UpdatedByUserId")) ? null : reader.GetString(reader.GetOrdinal("UpdatedByUserId")),
                            UpdatedOn = reader.IsDBNull(reader.GetOrdinal("UpdatedOn")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedOn")),
                            StatusId = reader.GetInt32(reader.GetOrdinal("StatusId"))
                        };

                        // ensure list exists (no-op if ProgramDef already has default)
                        programDef.UserProgAssignments = new List<UserProgAssignmentDto>();

                        programDefs.Add(programDef);
                    }


                    // Move to Result set #2: ProgramUserAssignments (for those ProgramIds)
                    var grouped = new Dictionary<int, List<UserProgAssignmentDto>>();

                    if (await reader.NextResultAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var progId = reader.GetInt32(reader.GetOrdinal("ProgramId"));
                            var userId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? string.Empty : reader.GetString(reader.GetOrdinal("UserId"));
                            var roleId = reader.IsDBNull(reader.GetOrdinal("RoleId")) ? 0 : reader.GetInt32(reader.GetOrdinal("RoleId"));
                            int? itemTenant = reader.IsDBNull(reader.GetOrdinal("TenantId")) ? null : reader.GetInt32(reader.GetOrdinal("TenantId"));

                            var dto = new UserProgAssignmentDto
                            {
                                UserId = userId,
                                RoleId = roleId,
                                TenantId = itemTenant
                            };

                            if (!grouped.TryGetValue(progId, out var list))
                            {
                                list = new List<UserProgAssignmentDto>();
                                grouped[progId] = list;
                            }
                            list.Add(dto);
                        }
                    }
                    // programs + assignments now populated

                    foreach (var p in programDefs)
                    {
                        if (grouped != null && grouped.ContainsKey(p.ProgramId))
                        {
                            p.UserProgAssignments = grouped[p.ProgramId];
                        }
                    }


                }


                _logger.LogInformation("Successfully fetched {Count} programs for TenantId={TenantId}", programDefs.Count, TenantId);
                return programDefs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch programs for TenantId={TenantId}", TenantId);
                return null;
            }
        }


        //CREATE PROGRAM METHOD
        public async Task CreateProgram(string UserId, ProgramDto data)
        {
            _logger.LogInformation("Creating Program for Assessment={AssessmentId}, Name={Title}, CreatedBy={UserId}",
                data.AssessmentId, data.Title, UserId);

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var table = new DataTable("UserProgAssignments");

            // Define schema
            table.Columns.Add("UserId", typeof(string));
            table.Columns.Add("TenantId", typeof(int));
            table.Columns.Add("RoleId", typeof(int));


            // Fill rows
            if (data.UserProgAssignments != null && data.UserProgAssignments.Count() > 0)
                foreach (var x in data.UserProgAssignments)
                {
                    // If you expect nulls for UserId, use (object?)x.UserId ?? DBNull.Value
                    table.Rows.Add(x.UserId, x.TenantId, x.RoleId);
                }

            await using var cmd = new SqlCommand("sp_CreateProgramV2", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@TenantId", data.TenantId);
            cmd.Parameters.AddWithValue("@Title", data.Title ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Details", data.Description ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@UserId", UserId ?? (object)DBNull.Value);

            var tvp = cmd.Parameters.AddWithValue("@Assign", table);
            tvp.SqlDbType = SqlDbType.Structured;
            tvp.TypeName = "dbo.UserTenantTvp";

            try
            {
                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var programId = reader.IsDBNull(0) ? (int?)null : reader.GetInt32(0);
                    var programCode = reader.FieldCount > 1 && !reader.IsDBNull(1) ? reader.GetString(1) : null;
                    _logger.LogInformation("Program created: Id={ProgramId}, Code={ProgramCode}", programId, programCode);
                }
                else
                {
                    _logger.LogInformation("sp_CreateProgram executed but returned no result row.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Program for Assessment={AssessmentId}", data.AssessmentId);
                throw;
            }
        }

   
        //UPDATE PROGRAM METHOD
        public async Task UpdateProgram(string UserId, ProgramDto data)
        {
            _logger.LogInformation("Updating Program ID={ProgramId}, Name={Name}, UpdatedBy={UserId}", data.ProgramId, data.Title, UserId);

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var table = new DataTable("UserProgAssignments");

                // Define schema
                table.Columns.Add("UserId", typeof(string));
                table.Columns.Add("TenantId", typeof(int));
                table.Columns.Add("RoleId", typeof(int));
                

                // Fill rows
                if(data.UserProgAssignments!=null && data.UserProgAssignments.Count() >0)
                    foreach (var x in data.UserProgAssignments)
                    {
                        // If you expect nulls for UserId, use (object?)x.UserId ?? DBNull.Value
                        table.Rows.Add(x.UserId, x.TenantId, x.RoleId);
                    }

                var cmd = new SqlCommand(@"sp_UpdateProgram", conn);

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@ProgramId", data.ProgramId.Value  );

                cmd.Parameters.AddWithValue("@TenantId", data.TenantId);
                cmd.Parameters.AddWithValue("@Title", data.Title ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Description", data.Description ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@StatusId", data.StatusId);
                cmd.Parameters.AddWithValue("@UpdatedByUserId", UserId );


                var tvp = cmd.Parameters.AddWithValue("@Assign", table);
                tvp.SqlDbType = SqlDbType.Structured;
                tvp.TypeName = "dbo.UserTenantTvp";


                var affected = await cmd.ExecuteNonQueryAsync();
                _logger.LogInformation("Program assignments sync executed for ProgramId={ProgramId}.", data.ProgramId);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Program ID={ProgramId}", data.ProgramId);
                throw;
            }

        }
        public async Task<List<ProgramDetails>> GetProgramsByMissionAsync(int? missionId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                _logger.LogDebug("Database connection opened successfully for MissionId={MissionId}", missionId);

                var sql = @"
                SELECT
                    ProgramId,
                    ProgramCode,
                    Title,
                    [Description],
                    CreatedOn,
                    UpdatedByUserId,
                    UpdatedOn,
                    StatusId
                FROM dbo.tbl_RegistrationPrograms
                WHERE (@MissionId IS NULL OR TenantId = @MissionId);";

                var programs = await conn.QueryAsync<ProgramDetails>(sql, new { MissionId = missionId });
                return programs.AsList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load programs for MissionId={MissionId}", missionId);
                return new List<ProgramDetails>();
            }
        }

    }
}
