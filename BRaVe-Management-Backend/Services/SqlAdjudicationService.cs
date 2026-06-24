using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BRaVe_Management_Backend.Services
{
    public class SqlAdjudicationService : IAdjudicationService
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlAdjudicationService> _logger;

        public SqlAdjudicationService(
            ISecretProvider secretProvider,
            ILogger<SqlAdjudicationService> logger)
        {
            _connectionString = secretProvider
                .GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection)
                .Result;

            _logger = logger;

            _logger.LogInformation("SqlAdjudicationService initialized with connection string from KeyVault");
        }

        public async Task<List<AdjudicationDecisionDto>>GetAdjudicationDecisionsAsync(CancellationToken cancellationToken = default)
        {
            const string sql = @"
                SELECT DecisionId,
                       LanguageCode,
                       Name,
                       Description
                FROM dbo.lkp_AdjudicationDecision_Translations
                WHERE LanguageCode = 'en' and decisionid!=1
                ORDER BY Name";

            _logger.LogInformation("Fetching adjudication decisions (LanguageCode = 'en')");

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            await using var cmd = new SqlCommand(sql, conn);

            try
            {
                await using var reader =
                    await cmd.ExecuteReaderAsync(cancellationToken);

                var results = new List<AdjudicationDecisionDto>();

                while (await reader.ReadAsync(cancellationToken))
                {
                    results.Add(new AdjudicationDecisionDto
                    {
                        DecisionId = reader.GetInt32(reader.GetOrdinal("DecisionId")),
                        LanguageCode = reader.GetString(reader.GetOrdinal("LanguageCode")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("Description"))
                    });
                }

                _logger.LogInformation(
                    "Successfully fetched {Count} adjudication decisions",
                    results.Count);

                return results;
            }
            catch (SqlException ex)
            {
                _logger.LogWarning(ex,
                    "Failed to fetch adjudication decisions");

                throw;
            }
        }


        public async Task<AdjudicationResultDto> AdjudicateAsync(AdjudicationRequestDto request,CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Starting adjudication JobId={JobId}, DecisionId={DecisionId}",
                request.JobId,
                request.DecisionId);

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            await using var cmd = new SqlCommand(
                "dbo.sp_AdjudicateDeduplication",
                conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            //--------------------------------------------------
            // PARAMETERS
            //--------------------------------------------------

            cmd.Parameters.Add("@TenantId", SqlDbType.Int)
                .Value = request.TenantId;

            cmd.Parameters.Add("@JobId", SqlDbType.BigInt)
                .Value = request.JobId;

            cmd.Parameters.Add("@SourceUuid", SqlDbType.UniqueIdentifier)
                .Value = request.SourceUuid;

            cmd.Parameters.Add("@MatchedUuid", SqlDbType.UniqueIdentifier)
                .Value = request.MatchedUuid;

            cmd.Parameters.Add("@DecisionId", SqlDbType.Int)
                .Value = request.DecisionId;

            cmd.Parameters.Add("@Suggestion", SqlDbType.NVarChar, 100)
                .Value = (object?)request.Suggestion ?? DBNull.Value;

            cmd.Parameters.Add("@Notes", SqlDbType.NVarChar, 500)
                .Value = request.Notes;

            cmd.Parameters.Add("@AdjudicatedBy", SqlDbType.VarChar, 50)
                .Value = request.AdjudicatedBy;

            cmd.Parameters.Add("@DedupMode", SqlDbType.Char, 1)
                .Value = request.DedupMode;

            //--------------------------------------------------
            // EXECUTION
            //--------------------------------------------------

            try
            {
                await using var reader =
                    await cmd.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                    throw new Exception("Stored procedure returned no result.");

                var result = new AdjudicationResultDto
                {
                    AdjudicationId = reader.GetInt64(
                        reader.GetOrdinal("AdjudicationId")),

                    SourceStatus = reader.GetString(
                        reader.GetOrdinal("SourceStatus")),

                    MatchedStatus = reader.GetString(
                        reader.GetOrdinal("MatchedStatus"))
                };

                _logger.LogInformation(
                    "Adjudication completed successfully. AdjudicationId={Id}",
                    result.AdjudicationId);

                return result;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex,
                    "SQL error during adjudication JobId={JobId}",
                    request.JobId);

                throw;
            }
        }



        //---------------------------------------
        // Bulk adjudication
        //---------------------------------------
        public async Task BulkAdjudicateAsync(List<AdjudicationRequestDto> requests,CancellationToken cancellationToken = default)
        {
            if (requests == null || requests.Count == 0)
                throw new ArgumentException("No adjudication requests provided.");

            var first = requests.First();

            _logger.LogInformation(
                "Starting BULK adjudication for {Count} records JobId={JobId}",
                requests.Count,
                first.JobId);

            //---------------------------------------
            // Build TVP DataTable
            //---------------------------------------

            var table = new DataTable();
            table.Columns.Add("SourceUuid", typeof(Guid));
            table.Columns.Add("MatchedUuid", typeof(Guid));

            foreach (var r in requests)
            {
                table.Rows.Add(r.SourceUuid, r.MatchedUuid);
            }

            //---------------------------------------
            // SQL
            //---------------------------------------

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            await using var cmd = new SqlCommand(
                "dbo.sp_AdjudicateDeduplicationBulk",
                conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            //---------------------------------------
            // PARAMETERS
            //---------------------------------------

            cmd.Parameters.Add("@TenantId", SqlDbType.Int).Value = first.TenantId;

            cmd.Parameters.Add("@JobId", SqlDbType.BigInt)
                .Value = (object?)first.JobId ?? DBNull.Value;

            cmd.Parameters.Add("@DecisionId", SqlDbType.Int).Value = first.DecisionId;

            cmd.Parameters.Add("@Suggestion", SqlDbType.NVarChar, 100)
                .Value = (object?)first.Suggestion ?? DBNull.Value;

            cmd.Parameters.Add("@Notes", SqlDbType.NVarChar, 500).Value = first.Notes;

            cmd.Parameters.Add("@AdjudicatedBy", SqlDbType.VarChar, 50)
                .Value = first.AdjudicatedBy;

            cmd.Parameters.Add("@DedupMode", SqlDbType.Char, 1)
                .Value = first.DedupMode;

            //---------------------------------------
            // TVP
            //---------------------------------------

            var tvp = cmd.Parameters.AddWithValue("@Pairs", table);
            tvp.SqlDbType = SqlDbType.Structured;
            tvp.TypeName = "dbo.DeduplicationPairType";

            //---------------------------------------
            // EXECUTE
            //---------------------------------------

            try
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken);

                _logger.LogInformation(
                    "Bulk adjudication completed successfully for {Count} records",
                    requests.Count);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex,
                    "SQL error during bulk adjudication JobId={JobId}",
                    first.JobId);

                throw;
            }
        }


        public async Task<List<MemberMatchHistoryDto>> GetMemberMatchHistoryAsync(Guid selectedUuid,CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Fetching match history for member {MemberUuid}",
                selectedUuid);

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            await using var cmd = new SqlCommand(
                "dbo.sp_GetMemberMatchHistory",
                conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@SelectedUuid", SqlDbType.UniqueIdentifier)
                .Value = selectedUuid;

            try
            {
                await using var reader =
                    await cmd.ExecuteReaderAsync(cancellationToken);

                var results = new List<MemberMatchHistoryDto>();

                while (await reader.ReadAsync(cancellationToken))
                {
                    results.Add(new MemberMatchHistoryDto
                    {
                        SelectedMemberUuid = reader.GetGuid(
                            reader.GetOrdinal("SelectedMemberUuid")),

                        OtherMemberUuid = reader.GetGuid(
                            reader.GetOrdinal("OtherMemberUuid")),

                        OtherMemberName = reader.GetString(
                            reader.GetOrdinal("OtherMemberName")),

                        OtherMemberAge = reader.IsDBNull(
                            reader.GetOrdinal("OtherMemberAge"))
                                ? null
                                : reader.GetInt32(reader.GetOrdinal("OtherMemberAge")),

                        OtherMemberGender = reader.GetString(
                            reader.GetOrdinal("OtherMemberGender")),

                        OtherMemberGps = reader.IsDBNull(
                            reader.GetOrdinal("GPS"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("GPS")),

                        ActivityCode = reader.IsDBNull(
                            reader.GetOrdinal("ActivityCode"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("ActivityCode")),

                        OtherMemberStatus = reader.GetString(
                            reader.GetOrdinal("OtherMemberStatus")),

                        AdjudicationId = reader.GetInt64(
                            reader.GetOrdinal("AdjudicationId")),

                        AdjudicatedOn = reader.GetDateTime(
                            reader.GetOrdinal("AdjudicatedOn"))
                    });
                }

                _logger.LogInformation(
                    "Fetched {Count} match history records for {MemberUuid}",
                    results.Count,
                    selectedUuid);

                return results;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex,
                    "SQL error fetching match history for member {MemberUuid}",
                    selectedUuid);

                throw;
            }
        }

        public async Task<PagedResultDto<AdjudicationHistoryDto>> GetAdjudicationHistoryAsync(
    int tenantId,
    int pageNumber,
    int pageSize,
    int? decisionId = null,
    string? dedupMode = null,
    DateTime? dateFrom = null,
    DateTime? dateTo = null,
    string? adjudicatedBy = null,
    CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Fetching adjudication history TenantId={TenantId}, Page={Page}",
                tenantId,
                pageNumber);

            await using var conn = new SqlConnection(_connectionString);

            await conn.OpenAsync(cancellationToken);

            await using var cmd = new SqlCommand(
                "dbo.sp_GetAdjudicationHistory",
                conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            //---------------------------------------
            // PARAMETERS
            //---------------------------------------

            cmd.Parameters.Add("@TenantId", SqlDbType.Int)
                .Value = tenantId;

            cmd.Parameters.Add("@PageNumber", SqlDbType.Int)
                .Value = pageNumber;

            cmd.Parameters.Add("@PageSize", SqlDbType.Int)
                .Value = pageSize;

            cmd.Parameters.Add("@DecisionId", SqlDbType.Int)
                .Value = (object?)decisionId ?? DBNull.Value;

            cmd.Parameters.Add("@DedupMode", SqlDbType.Char, 1)
                .Value = (object?)dedupMode ?? DBNull.Value;

            cmd.Parameters.Add("@DateFrom", SqlDbType.Date)
                .Value = (object?)dateFrom ?? DBNull.Value;

            cmd.Parameters.Add("@DateTo", SqlDbType.Date)
                .Value = (object?)dateTo ?? DBNull.Value;

            cmd.Parameters.Add("@AdjudicatedBy", SqlDbType.NVarChar, 50)
                .Value = (object?)adjudicatedBy ?? DBNull.Value;

            try
            {
                await using var reader =
                    await cmd.ExecuteReaderAsync(cancellationToken);

                //---------------------------------------
                // TOTAL COUNT
                //---------------------------------------

                int totalCount = 0;

                if (await reader.ReadAsync(cancellationToken))
                {
                    totalCount = reader.GetInt32(
                        reader.GetOrdinal("TotalCount"));
                }

                //---------------------------------------
                // NEXT RESULT
                //---------------------------------------

                await reader.NextResultAsync(cancellationToken);

                var results = new List<AdjudicationHistoryDto>();

                //---------------------------------------
                // DATA
                //---------------------------------------

                while (await reader.ReadAsync(cancellationToken))
                {
                    results.Add(new AdjudicationHistoryDto
                    {
                        HistoryId = reader.GetInt64(
                            reader.GetOrdinal("HistoryId")),

                        AdjudicationId = reader.GetInt64(
                            reader.GetOrdinal("AdjudicationId")),

                        SourceUuid = reader.GetGuid(
                            reader.GetOrdinal("Source UUID")),

                        MatchedUuid = reader.IsDBNull(
                            reader.GetOrdinal("Matched UUID"))
                                ? null
                                : reader.GetGuid(reader.GetOrdinal("Matched UUID")),

                        SourceFullName = reader.GetString(
                            reader.GetOrdinal("Source Full Name")),

                        SourceAge = reader.IsDBNull(
                            reader.GetOrdinal("Source Age"))
                                ? null
                                : reader.GetInt32(reader.GetOrdinal("Source Age")),

                        SourceGender = reader.GetString(
                            reader.GetOrdinal("Source Gender")),

                        SourceRelationship = reader.IsDBNull(
                            reader.GetOrdinal("Source Relationship"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("Source Relationship")),

                        SourceHouseholdId = reader.IsDBNull(
                            reader.GetOrdinal("Source Household Id"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("Source Household Id")),

                        MatchedFullName = reader.IsDBNull(
                            reader.GetOrdinal("Matched Full Name"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("Matched Full Name")),

                        MatchedAge = reader.IsDBNull(
                            reader.GetOrdinal("Matched Age"))
                                ? null
                                : reader.GetInt32(reader.GetOrdinal("Matched Age")),

                        MatchedGender = reader.IsDBNull(
                            reader.GetOrdinal("Matched Gender"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("Matched Gender")),

                        MatchedRelationship = reader.IsDBNull(
                            reader.GetOrdinal("Matched Relationship"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("Matched Relationship")),

                        MatchedHouseholdId = reader.IsDBNull(
                            reader.GetOrdinal("Matched Household Id"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("Matched Household Id")),

                         MatchScore = reader.IsDBNull(
                                reader.GetOrdinal("MatchScore"))
                                    ? null
                                    : Convert.ToInt32(reader["MatchScore"]),

                        MatchProbability = reader.IsDBNull(
                            reader.GetOrdinal("Manual Match Probability"))
                                ? null
                                : reader.GetDecimal(reader.GetOrdinal("Manual Match Probability")),

                        MatchType = reader.GetString(
                            reader.GetOrdinal("Match Type")),

                        DecisionId = reader.GetInt32(
                            reader.GetOrdinal("DecisionId")),

                        DecisionName = reader.GetString(
                            reader.GetOrdinal("Decision Name")),

                        DecisionDescription = reader.IsDBNull(
                            reader.GetOrdinal("Decision Description"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("Decision Description")),

                        DedupMode = reader.GetString(
                            reader.GetOrdinal("DedupMode")),

                        DedupModeName = reader.GetString(
                            reader.GetOrdinal("Dedup Mode Name")),

                        InitialState = reader.GetString(
                            reader.GetOrdinal("InitialState")),

                        InitialStateName = reader.GetString(
                            reader.GetOrdinal("InitialStateName")),

                        FinalState = reader.GetString(
                            reader.GetOrdinal("FinalState")),

                        FinalStateName = reader.GetString(
                            reader.GetOrdinal("FinalStateName")),

                        AdjudicatedBy = reader.IsDBNull(
                            reader.GetOrdinal("AdjudicatedBy"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("AdjudicatedBy")),

                        AdjudicatedOn = reader.GetDateTime(
                            reader.GetOrdinal("AdjudicatedOn")),

                        Suggestion = reader.IsDBNull(
                            reader.GetOrdinal("Suggestion"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("Suggestion")),

                        Notes = reader.IsDBNull(
                            reader.GetOrdinal("Notes"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("Notes"))
                    });
                }

                _logger.LogInformation(
                    "Fetched {Count} adjudication history records",
                    results.Count);

                return new PagedResultDto<AdjudicationHistoryDto>
                {
                    Items = results,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    "SQL error fetching adjudication history");

                throw;
            }
        }
    }
}