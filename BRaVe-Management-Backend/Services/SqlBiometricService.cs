using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;


namespace BRaVe_Management_Backend.Services
{
    public class SqlBiometricService : ISqlBiometricService
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlBiometricService> _logger;

        public SqlBiometricService(
            ISecretProvider secretProvider,
            ILogger<SqlBiometricService> logger)
        {
            _logger = logger;
            _connectionString =
                secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection).Result;
        }

        public async Task<PagedResult<BiometricMatchListDto>>GetBiometricMatchListAsync(int tenantId,int pageNumber,int pageSize,string? activity,DateTime? dateFrom, DateTime? dateTo, int? minScore)
        {
            _logger.LogInformation(
                "GetBiometricMatchListAsync started | TenantId={TenantId}, PageNumber={PageNumber}, PageSize={PageSize}, Activity={Activity}, dateFrom={dateFrom},dateTo={dateTo}, MinScore={MinScore}",
                tenantId, pageNumber, pageSize, activity, dateFrom, dateTo, minScore);

            var result = new PagedResult<BiometricMatchListDto>();

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                await using var cmd = new SqlCommand("dbo.sp_GetBiometricMatchList", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.Add("@TenantId", SqlDbType.Int).Value = tenantId;
                cmd.Parameters.Add("@PageNumber", SqlDbType.Int).Value = pageNumber;
                cmd.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;

                cmd.Parameters.Add("@Activity", SqlDbType.NVarChar, 50)
                    .Value = string.IsNullOrWhiteSpace(activity)
                        ? DBNull.Value
                        : activity;

                cmd.Parameters.Add("@DateFrom", SqlDbType.Date)
                    .Value = dateFrom.HasValue
                        ? dateFrom.Value
                        : DBNull.Value;
                cmd.Parameters.Add("@DateTo", SqlDbType.Date)
                    .Value = dateTo.HasValue
                        ? dateTo.Value
                        : DBNull.Value;

                cmd.Parameters.Add("@MinScore", SqlDbType.Int)
                    .Value = minScore.HasValue
                        ? minScore.Value
                        : DBNull.Value;

                await using var reader = await cmd.ExecuteReaderAsync();

                // TOTAL COUNT
                if (await reader.ReadAsync())
                {
                    result.TotalCount = reader.GetInt32(0);
                }

                await reader.NextResultAsync();

                // DATA
                while (await reader.ReadAsync())
                {
                    result.Items.Add(new BiometricMatchListDto
                    {
                        SourceActivity = reader["Source Activity"]?.ToString(),
                        SourceUuid = reader.GetGuid(reader.GetOrdinal("Source UUID")),
                        SourceFullName = reader["Source Full Name"]?.ToString(),
                        SourceAge = reader.IsDBNull(reader.GetOrdinal("Source Age"))
                            ? null
                            : reader.GetInt32(reader.GetOrdinal("Source Age")),
                        SourceGender = reader["Source Gender"]?.ToString(),
                        SourceRelationship = reader["Source Relationship"]?.ToString(),
                        SourceHouseholdId = reader["Source Household Id"]?.ToString(),
                        SourceRegistrationDate =
                            reader.GetDateTime(reader.GetOrdinal("Source Registration Date")),

                        MatchedActivity = reader["Matched Activity"]?.ToString(),
                        MatchedUuid = reader.GetGuid(reader.GetOrdinal("Matched UUID")),
                        MatchedFullName = reader["Matched Full Name"]?.ToString(),
                        MatchedAge = reader.IsDBNull(reader.GetOrdinal("Matched Age"))
                            ? null
                            : reader.GetInt32(reader.GetOrdinal("Matched Age")),
                        MatchedGender = reader["Matched Gender"]?.ToString(),
                        MatchedRelationship = reader["Matched Relationship"]?.ToString(),
                        MatchedHouseholdId = reader["Matched Household Id"]?.ToString(),
                        MatchedRegistrationDate =
                            reader.GetDateTime(reader.GetOrdinal("Matched Registration Date")),

                        MatchScore = reader.GetInt32(reader.GetOrdinal("Match Score")),
                        Status = reader.GetInt32(reader.GetOrdinal("Status")),
                        MatchedOn = reader.GetDateTime(reader.GetOrdinal("MatchedOn"))
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error in GetBiometricMatchListAsync | TenantId={TenantId}",
                    tenantId);

                throw;
            }
        }

        public async Task<BiometricMatchResultDto?> GetBiometricMatchDetailsAsync(int tenantId,Guid sourceUuid,Guid matchedUuid)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(
                "dbo.sp_GetBiometricMatchDetails",
                conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@TenantId", SqlDbType.Int).Value = tenantId;
            cmd.Parameters.Add("@SourceUuid", SqlDbType.UniqueIdentifier).Value = sourceUuid;
            cmd.Parameters.Add("@MatchedUuid", SqlDbType.UniqueIdentifier).Value = matchedUuid;

            await using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new BiometricMatchResultDto
            {
                // SOURCE
                SourceUuid = reader.GetGuid(reader.GetOrdinal("Source UUID")),
                SourceFullName = reader["Source Full Name"]?.ToString(),
                SourceAge = reader.IsDBNull(reader.GetOrdinal("Source Age"))
                    ? null
                    : reader.GetInt32(reader.GetOrdinal("Source Age")),
                SourceGender = reader["Source Gender"]?.ToString(),
                SourceRelationship = reader["Source Relationship"]?.ToString(),
                SourceHouseholdId = reader["Source Household Id"]?.ToString(),
                SourceHHSize = reader.IsDBNull(reader.GetOrdinal("Source HHSize"))
                    ? null
                    : reader.GetInt32(reader.GetOrdinal("Source HHSize")),
                SourcePhoto = reader["Source Photo"]?.ToString(),
                SourceMission = reader["Source Mission"]?.ToString(),
                SourceGps = reader["Source Gps"]?.ToString(),
                SourceRegistrationDate =
                    reader.GetDateTime(reader.GetOrdinal("Source Registration Date")),

                // MATCHED
                MatchedUuid = reader.GetGuid(reader.GetOrdinal("Matched UUID")),
                MatchedFullName = reader["Matched Full Name"]?.ToString(),
                MatchedAge = reader.IsDBNull(reader.GetOrdinal("Matched Age"))
                    ? null
                    : reader.GetInt32(reader.GetOrdinal("Matched Age")),
                MatchedGender = reader["Matched Gender"]?.ToString(),
                MatchedRelationship = reader["Matched Relationship"]?.ToString(),
                MatchedHouseholdId = reader["Matched Household Id"]?.ToString(),
                MatchedHHSize = reader.IsDBNull(reader.GetOrdinal("Matched HHSize"))
                    ? null
                    : reader.GetInt32(reader.GetOrdinal("Matched HHSize")),
                MatchedPhoto = reader["Matched Photo"]?.ToString(),
                MatchedMission = reader["Matched Mission"]?.ToString(),
                MatchedGps = reader["Matched Gps"]?.ToString(),
                MatchedRegistrationDate =
                    reader.GetDateTime(reader.GetOrdinal("Matched Registration Date")),

                // MATCH INFO
                MatchScore = reader.GetInt32(reader.GetOrdinal("Match Score")),
                Status = reader.GetInt32(reader.GetOrdinal("Status")),
                MatchedOn = reader.GetDateTime(reader.GetOrdinal("MatchedOn"))
            };
        }

        public async Task<BiometricMatchResultDto?> GetManualMatchDetailsAsync(
            int tenantId,
            Guid sourceUuid,
            Guid matchedUuid)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("sp_GetManualMatchDetails", conn);

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@TenantId", tenantId);
                cmd.Parameters.AddWithValue("@SourceUuid", sourceUuid);
                cmd.Parameters.AddWithValue("@MatchedUuid", matchedUuid);

                await conn.OpenAsync();

                using var reader = await cmd.ExecuteReaderAsync();

                if (!reader.HasRows)
                    return null;

                await reader.ReadAsync();

                var result = new BiometricMatchResultDto
                {
                    // SOURCE
                    SourceUuid = reader["Source UUID"] as Guid?,
                    SourceFullName = reader["Source Full Name"]?.ToString(),
                    SourceAge = reader["Source Age"] as int?,
                    SourceGender = reader["Source Gender"]?.ToString(),
                    SourceRelationship = reader["Source Relationship"]?.ToString(),
                    SourceHouseholdId = reader["Source Household Id"]?.ToString(),
                    SourceHHSize = reader["Source HHSize"] as int?,
                    SourcePhoto = reader["Source Photo"]?.ToString(),
                    SourceMission = reader["Source Mission"]?.ToString(),
                    SourceGps = reader["Source Gps"]?.ToString(),
                    SourceRegistrationDate = reader["Source Registration Date"] as DateTime?,

                    // MATCHED
                    MatchedUuid = (Guid)reader["Matched UUID"],
                    MatchedFullName = reader["Matched Full Name"]?.ToString(),
                    MatchedAge = reader["Matched Age"] as int?,
                    MatchedGender = reader["Matched Gender"]?.ToString(),
                    MatchedRelationship = reader["Matched Relationship"]?.ToString(),
                    MatchedHouseholdId = reader["Matched Household Id"]?.ToString(),
                    MatchedHHSize = reader["Matched HHSize"] as int?,
                    MatchedPhoto = reader["Matched Photo"]?.ToString(),
                    MatchedMission = reader["Matched Mission"]?.ToString(),
                    MatchedGps = reader["Matched Gps"]?.ToString(),
                    MatchedRegistrationDate = reader["Matched Registration Date"] as DateTime? ?? DateTime.MinValue,

                    // MATCH INFO
                    MatchScore = reader["Match Score"] != DBNull.Value ? Convert.ToInt32(reader["Match Score"]) : 0,
                    Status = reader["Status"] != DBNull.Value ? Convert.ToInt32(reader["Status"]) : 0,
                    MatchedOn = reader["MatchedOn"] as DateTime? ?? DateTime.MinValue,

                    IsManual = true
                };

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving manual match details.", ex);
            }
        }


        public async Task<PagedResult<BiometricVerificationResultDto>>GetBiometricVerificationsAsync(int tenantId,int pageNumber,int pageSize)
        {
            _logger.LogInformation(
                "GetBiometricVerificationsAsync started | TenantId={TenantId}, PageNumber={PageNumber}, PageSize={PageSize}",
                tenantId, pageNumber, pageSize);

            var items = new List<BiometricVerificationResultDto>();
            int totalCount = 0;
            int rowCount = 0;

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                _logger.LogInformation("Executing stored procedure dbo.sp_GetBiometricVerifications");

                await using var cmd = new SqlCommand(
                    "dbo.sp_GetBiometricVerifications",
                    conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@TenantId", tenantId);
                cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);

                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    rowCount++;

                    if (totalCount == 0)
                    {
                        totalCount = reader.GetInt32(
                            reader.GetOrdinal("TotalCount"));
                    }

                    items.Add(new BiometricVerificationResultDto
                    {
                        JobId = reader["JobId"]?.ToString(),
                        ActivityCode = reader.IsDBNull(reader.GetOrdinal("ActivityCode"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("ActivityCode")),
                        BiometricId = reader.GetGuid(reader.GetOrdinal("BiometricId")),
                        ProcessedOn = reader.GetDateTime(reader.GetOrdinal("TransferedOn")),
                        Match = reader["Match"]?.ToString(),
                        FullName = reader["Full Name"]?.ToString(),
                        Age = reader.IsDBNull(reader.GetOrdinal("Age"))
                            ? null
                            : reader.GetInt32(reader.GetOrdinal("Age")),
                        Gender = reader["Gender"]?.ToString(),
                        Relationship = reader.IsDBNull(reader.GetOrdinal("Relationship"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("Relationship")),
                        HouseholdId = reader["Household Id"]?.ToString(),
                        Photo = reader["Photo"]?.ToString(),
                        Mission = reader["Mission"]?.ToString(),
                        RegistrationDate = reader.IsDBNull(reader.GetOrdinal("Registration Date"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("Registration Date"))
                    });
                }

                _logger.LogInformation(
                    "GetBiometricVerificationsAsync completed | RowsReturned={RowCount}, TotalCount={TotalCount}",
                    rowCount, totalCount);

                return new PagedResult<BiometricVerificationResultDto>
                {
                    Items = items,
                    TotalCount = totalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error in GetBiometricVerificationsAsync | TenantId={TenantId}, PageNumber={PageNumber}, PageSize={PageSize}",
                    tenantId, pageNumber, pageSize);

                throw;
            }
        }



        //Get duplicate indicator list
        public async Task<List<DuplicateIndicatorChecklistDto>> GetDuplicateIndicatorChecklistAsync(int? tenantId, string languageCode)
        {
            _logger.LogInformation(
                "GetDuplicateIndicatorChecklistAsync started | TenantId={TenantId}, LanguageCode={LanguageCode}",
                tenantId, languageCode);

            var items = new List<DuplicateIndicatorChecklistDto>();

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                await using var cmd = new SqlCommand(
                    "dbo.sp_GetDuplicateIndicatorChecklist",
                    conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.Add("@TenantId", SqlDbType.Int)
                    .Value = tenantId.HasValue ? tenantId.Value : DBNull.Value;

                cmd.Parameters.Add("@LanguageCode", SqlDbType.NVarChar, 10)
                    .Value = string.IsNullOrWhiteSpace(languageCode)
                        ? "en"
                        : languageCode;

                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    items.Add(new DuplicateIndicatorChecklistDto
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        IndicatorId = reader.GetInt32(reader.GetOrdinal("IndicatorId")),
                        ExclusiveGroupId = reader.GetInt32(reader.GetOrdinal("ExclusiveGroupId")),
                        Score = reader.GetDecimal(reader.GetOrdinal("Score")),
                        TenantId = reader.IsDBNull(reader.GetOrdinal("TenantId"))
                            ? null
                            : reader.GetInt32(reader.GetOrdinal("TenantId")),
                        Name = reader["Name"]?.ToString()
                    });
                }

                _logger.LogInformation(
                    "GetDuplicateIndicatorChecklistAsync completed | RowsReturned={Count}",
                    items.Count);

                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error in GetDuplicateIndicatorChecklistAsync | TenantId={TenantId}",
                    tenantId);

                throw;
            }
        }

//Get programatic data
        public async Task<List<ProgrammaticDataDto>> GetProgrammaticDataAsync(int tenantId,string householdId)
        {
            _logger.LogInformation(
                "GetProgrammaticDataAsync started | TenantId={TenantId}, HouseholdId={HouseholdId}",
                tenantId, householdId);

            var items = new List<ProgrammaticDataDto>();

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                await using var cmd = new SqlCommand(
                    "dbo.sp_GetProgrammaticData",
                    conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.Add("@TenantId", SqlDbType.Int).Value = tenantId;
                cmd.Parameters.Add("@HouseholdId", SqlDbType.VarChar, 50).Value = householdId;

                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var item = new ProgrammaticDataDto();

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        string columnName = reader.GetName(i);
                        object? value = reader.IsDBNull(i) ? null : reader.GetValue(i);

                        if (columnName == "Id")
                            item.Id = Convert.ToInt32(value);
                        else
                            item.Fields[columnName] = value;
                    }

                    items.Add(item);
                }


                _logger.LogInformation(
                    "GetProgrammaticDataAsync completed | RowsReturned={Count}",
                    items.Count);

                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error in GetProgrammaticDataAsync | TenantId={TenantId}, HouseholdId={HouseholdId}",
                    tenantId, householdId);

                throw;
            }
        }

        public async Task<List<string>> GetAvailableMatchActivitiesAsync(int tenantId)
        {
            var activities = new List<string>();

            const string sql = @"
        SELECT DISTINCT activityCode
        FROM
        (
            SELECT SH.activityCode
            FROM tbl_BiometricMatches BM
            INNER JOIN tbl_Individuals SI
                ON SI.uuid = BM.BiometricUuid
            LEFT JOIN tbl_Households SH
                ON SH.householdId = SI.householdId
            WHERE (@TenantId = 0 OR BM.TenantId = @TenantId)
              AND SI.Status = 'U'

            UNION

            SELECT MH.activityCode
            FROM tbl_BiometricMatches BM
            INNER JOIN tbl_Individuals MI
                ON MI.uuid = BM.MatchedUuid
            LEFT JOIN tbl_Households MH
                ON MH.householdId = MI.householdId
            WHERE (@TenantId = 0 OR BM.TenantId = @TenantId)
        ) A
        WHERE activityCode IS NOT NULL
        ORDER BY activityCode";

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                await using var cmd = new SqlCommand(sql, conn);

                cmd.Parameters.Add("@TenantId", SqlDbType.Int).Value = tenantId;

                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    activities.Add(reader["activityCode"].ToString());
                }

                return activities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error retrieving available biometric match activities | TenantId={TenantId}",
                    tenantId);

                throw;
            }
        }

        public async Task<Guid> RemoveBiometricMatchingRequest(int tenantId, Guid uuid)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("sp_RemoveBiometricMatchingRequest", conn);

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@TenantId", tenantId);
                cmd.Parameters.AddWithValue("@MatchedUuid", uuid);

                // OUTPUT parameter
                var jobIdParam = new SqlParameter("@JobId", SqlDbType.UniqueIdentifier)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(jobIdParam);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                // Handle NULL jobId from SQL
                if (jobIdParam.Value == DBNull.Value)
                    return Guid.Empty;

                return (Guid)jobIdParam.Value;

            }
            catch (Exception ex)
            {
                throw new Exception("Error while removing biometric matching request from database.", ex);
            }
        }

        public async Task<Guid> ReprocessBiometricMatchingRequest(int tenantId, Guid uuid)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("sp_ReprocessBiometricMatchingRequest", conn);

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@TenantId", tenantId);
                cmd.Parameters.AddWithValue("@SourceUuid", uuid);

                // OUTPUT parameter
                var jobIdParam = new SqlParameter("@JobId", SqlDbType.UniqueIdentifier)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(jobIdParam);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                // Handle NULL jobId from SQL
                if (jobIdParam.Value == DBNull.Value)
                    return Guid.Empty;

                return (Guid)jobIdParam.Value;

            }
            catch (Exception ex)
            {
                throw new Exception("Error while reprocessing biometric matching request from database.", ex);
            }
        }
    }
}
