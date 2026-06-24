using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BRaVe_Management_Backend.Services
{
    public class SqlDashboardService : IDashboardService
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlDashboardService> _logger;

        public SqlDashboardService(
            ISecretProvider secretProvider,
            ILogger<SqlDashboardService> logger)
        {
            _logger = logger;
            _connectionString = secretProvider
                .GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection)
                .Result;
        }


        public async Task<IEnumerable<DashboardRowDto>> GetDashboardDataAsync(int tenantId,int? programId = null,int? activityId = null,DateTime? startDate = null,DateTime? endDate = null)
        {
            var results = new List<DashboardRowDto>();

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                await using var cmd = new SqlCommand("dbo.sp_GetDashboardRawData", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.Add("@TenantId", SqlDbType.Int).Value = tenantId;

                cmd.Parameters.Add("@ProgramId", SqlDbType.Int).Value =
                    (object?)programId ?? DBNull.Value;

                cmd.Parameters.Add("@ActivityId", SqlDbType.Int).Value =
                    (object?)activityId ?? DBNull.Value;

                cmd.Parameters.Add("@StartDate", SqlDbType.Date).Value =
                    (object?)startDate ?? DBNull.Value;

                cmd.Parameters.Add("@EndDate", SqlDbType.Date).Value =
                    (object?)endDate ?? DBNull.Value;

                await using var reader = await cmd.ExecuteReaderAsync();

                // Ordinals – MUST match column aliases EXACTLY
                int oMissionName = reader.GetOrdinal("MissionName");
                int oProgramID = reader.GetOrdinal("ProgramID");
                int oProgramName = reader.GetOrdinal("ProgramName");
                int oActivityCode = reader.GetOrdinal("ActivityCode");
                int oActivityTitle = reader.GetOrdinal("ActivityTitle");
                int oBeneficiaryId = reader.GetOrdinal("BeneficiaryID");

                int oHouseholdId = reader.GetOrdinal("HouseholdId");
                int oHouseholdSize = reader.GetOrdinal("HouseholdSize");
                int oHouseHoldType = reader.GetOrdinal("HouseHoldType");
                int oCreatedOn = reader.GetOrdinal("TransferDate");

                int oHeadFullName = reader.GetOrdinal("HeadFullName");
                int oHeadGender = reader.GetOrdinal("HeadGender");
                int oHeadAgeYears = reader.GetOrdinal("HeadAgeYears");
                int oHeadFingerprintCollected = reader.GetOrdinal("HeadFingerprintCollected");
                int oIsFlagged = reader.GetOrdinal("IsFlagged");

                int oHouseholdSurveys = reader.GetOrdinal("HouseholdSurveys");

                while (await reader.ReadAsync())
                {
                    results.Add(new DashboardRowDto
                    {
                        MissionName = reader.IsDBNull(oMissionName)
                            ? null
                            : reader.GetString(oMissionName),

                        ProgramID = reader.IsDBNull(oProgramID)
                            ? 0
                            : reader.GetInt32(oProgramID),

                        ProgramName = reader.IsDBNull(oProgramName)
                            ? null
                            : reader.GetString(oProgramName),

                        ActivityCode = reader.IsDBNull(oActivityCode)
                            ? null
                            : reader.GetString(oActivityCode),

                        ActivityTitle = reader.IsDBNull(oActivityTitle)
                            ? null
                            : reader.GetString(oActivityTitle),
                        BeneficiaryId = reader.IsDBNull(oBeneficiaryId)
                                ? Guid.Empty
                                : reader.GetGuid(oBeneficiaryId),

                        HouseholdId = reader.IsDBNull(oHouseholdId)
                            ? null
                            : reader.GetString(oHouseholdId),

                        HouseholdSize = reader.IsDBNull(oHouseholdSize)
                            ? 0
                            : reader.GetInt32(oHouseholdSize),

                        HouseHoldType = reader.IsDBNull(oHouseHoldType)
                            ? null
                            : reader.GetString(oHouseHoldType),

                        HeadFullName = reader.IsDBNull(oHeadFullName)
                            ? null
                            : reader.GetString(oHeadFullName),

                        HeadGender = reader.IsDBNull(oHeadGender)
                            ? null
                            : reader.GetString(oHeadGender),

                        HeadAgeYears = reader.IsDBNull(oHeadAgeYears)
                            ? (int?)null
                            : reader.GetInt32(oHeadAgeYears),

                        HeadFingerprintCollected = !reader.IsDBNull(oHeadFingerprintCollected)
                             && reader.GetBoolean(oHeadFingerprintCollected),
                        IsFlagged = !reader.IsDBNull(oIsFlagged)
                                && reader.GetBoolean(oIsFlagged),

                        HouseholdSurveys = reader.IsDBNull(oHouseholdSurveys)
                            ? (int?)null
                            : reader.GetInt32(oHouseholdSurveys),
                        CreatedOn = reader.IsDBNull(oCreatedOn)
                            ? (DateTime?)null
                            : reader.GetDateTime(oCreatedOn),
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing dbo.sp_GetDashboardRawData");
                throw;
            }

            return results;
        }




        public async Task<IEnumerable<DashboardSurveyDetailDto>> GetDashboardSurveyDetailsAsync(string? householdId,int? individualId)
        {
            var results = new List<DashboardSurveyDetailDto>();

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                await using var cmd = new SqlCommand("dbo.sp_GetDashboardSurveyDetails", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@HouseholdId",
                string.IsNullOrWhiteSpace(householdId) ? DBNull.Value : householdId);

                cmd.Parameters.AddWithValue("@IndividualId",
                    individualId.HasValue ? individualId.Value : DBNull.Value);

                await using var reader = await cmd.ExecuteReaderAsync();

                // Ordinals MUST match SP aliases
                int oHouseholdId = reader.GetOrdinal("HouseholdId");
                int oIndividualId = reader.GetOrdinal("IndividualId");
                int oQuestionId = reader.GetOrdinal("QuestionId");
                int oAnswerType = reader.GetOrdinal("AnswerType");
                int oQuestion = reader.GetOrdinal("Question");
                int oQuestionAnswer = reader.GetOrdinal("QuestionAnswer");
                int oSurveyId = reader.GetOrdinal("SurveyId");
                int oSurveyName = reader.GetOrdinal("Survey Name");


                while (await reader.ReadAsync())
                {
                    results.Add(new DashboardSurveyDetailDto
                    {
                        HouseholdId = reader.IsDBNull(oHouseholdId)
                            ? null
                            : reader.GetString(oHouseholdId),

                        IndividualId = reader.IsDBNull(oIndividualId)
                            ? (int?)null
                            : reader.GetInt32(oIndividualId),

                        QuestionId = reader.GetInt32(oQuestionId),

                        AnswerType = reader.IsDBNull(oAnswerType)
                            ? 0
                            : reader.GetInt32(oAnswerType),

                        Question = reader.IsDBNull(oQuestion)
                            ? null
                            : reader.GetString(oQuestion),

                        QuestionAnswer = reader.IsDBNull(oQuestionAnswer)
                            ? null
                            : reader.GetString(oQuestionAnswer),

                        SurveyId = reader.GetInt32(oSurveyId),

                        SurveyName = reader.IsDBNull(oSurveyName)
                            ? null
                            : reader.GetString(oSurveyName)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error executing dbo.sp_GetDashboardSurveyDetails for HouseholdId {HouseholdId}, IndividualId {IndividualId}",
                    householdId, individualId);
                throw;
            }

            return results;
        }

        public async Task<HouseholdDto?> GetHouseholdByIdAsync(string householdId, int tenantId)
        {
            if (string.IsNullOrWhiteSpace(householdId))
                return null;

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                const string sql = @"
       SELECT [householdId]
      ,[activityCode]
      ,[householdSize]
      ,CL.Text as householdType
      ,m.[name] as MissionName
      ,h.[CreatedByUserId] as CreatedBy
      ,h.[CreatedOn]
  FROM [dbo].[tbl_Households] h
  LEFT JOIN tbl_Missions M on M.Missionid=h.tenantid
  LEFT JOIN [tbl_CustomLookupTableValues] CL ON CL.lookupid=3 and (CL.TenantId =@TenantId or @TenantId=0) and CL.ID=H.[householdType]
  WHERE  [householdId]=@id";

                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add("@id", SqlDbType.VarChar).Value = householdId;
                cmd.Parameters.Add("@TenantId", SqlDbType.Int).Value = tenantId;

                await using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                    return null;

                return new HouseholdDto
                {
                    HouseholdId = reader.GetString(reader.GetOrdinal("householdId")),

                    ActivityCode = reader.IsDBNull(reader.GetOrdinal("activityCode"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("activityCode")),

                    HouseholdSize = reader.IsDBNull(reader.GetOrdinal("householdSize"))
                        ? 0
                        : reader.GetInt32(reader.GetOrdinal("householdSize")),

                    HouseholdType = reader.IsDBNull(reader.GetOrdinal("householdType"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("householdType")),

                    MissionName = reader.IsDBNull(reader.GetOrdinal("MissionName"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("MissionName")),

                    CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("CreatedBy")),

                    CreatedOn = reader.IsDBNull(reader.GetOrdinal("CreatedOn"))
                        ? (DateTime?)null
                        : reader.GetDateTime(reader.GetOrdinal("CreatedOn"))
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error fetching household details for HouseholdId {HouseholdId}",
                    householdId);
                throw;
            }
        }

        public async Task<IEnumerable<HouseholdMemberDto>> GetHouseholdMembersAsync(string householdId)
        {
            var results = new List<HouseholdMemberDto>();

            if (string.IsNullOrWhiteSpace(householdId))
                return results;

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand("dbo.sp_GetHouseholdMembers", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@HouseholdId", SqlDbType.VarChar, 50).Value = householdId;

            await using var reader = await cmd.ExecuteReaderAsync();

            // Ordinals (match SP exactly)
            int oIndividualId = reader.GetOrdinal("individualId");
            int oFullName = reader.GetOrdinal("FullName");
            int oAgeYears = reader.GetOrdinal("AgeYears");
            int oGender = reader.GetOrdinal("Gender");
            int oRelationship = reader.GetOrdinal("Relationship");
            int oPhoto = reader.GetOrdinal("Photo");
            int oFingerprint = reader.GetOrdinal("Fingerprint");
            int oIndividualSurveys = reader.GetOrdinal("IndividualSurveys");
            int oCreatedOn = reader.GetOrdinal("CreatedOn");

            while (await reader.ReadAsync())
            {
                results.Add(new HouseholdMemberDto
                {
                    IndividualId = reader.GetInt32(oIndividualId),

                    FullName = reader.IsDBNull(oFullName)
                        ? null
                        : reader.GetString(oFullName),

                    AgeYears = reader.IsDBNull(oAgeYears)
                        ? (int?)null
                        : reader.GetInt32(oAgeYears),

                    Gender = reader.IsDBNull(oGender)
                        ? null
                        : reader.GetString(oGender),

                    Relationship = reader.IsDBNull(oRelationship)
                        ? null
                        : reader.GetString(oRelationship),

                    Photo = reader.IsDBNull(oPhoto)
                        ? null
                        : reader.GetString(oPhoto),

                    Fingerprint = reader.IsDBNull(oFingerprint)
                        ? null
                        : reader.GetString(oFingerprint),

                    IndividualSurveys = reader.IsDBNull(oIndividualSurveys)
                        ? 0
                        : reader.GetInt32(oIndividualSurveys),

                    CreatedOn = reader.IsDBNull(oCreatedOn)
                        ? DateTime.MinValue
                        : reader.GetDateTime(oCreatedOn)
                });
            }

            return results;
        }


        public async Task<DashboardGraphDto> GetDashboardGraphDataAsync(
      int? tenantId,
      int? programId,
      DateTime? startDate,
      DateTime? endDate)
        {
            _logger.LogInformation(
                "Loading dashboard graph data. TenantId={TenantId}, ProgramId={ProgramId}, StartDate={StartDate}, EndDate={EndDate}",
                tenantId, programId, startDate, endDate);

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                _logger.LogInformation("SQL connection opened successfully");

                var parameters = new DynamicParameters();
                parameters.Add("@TenantId", tenantId == 0 ? null : tenantId);
                parameters.Add("@ProgramId", programId == 0 ? null : programId);
                parameters.Add("@StartDate", startDate);
                parameters.Add("@EndDate", endDate);

                _logger.LogDebug("Executing sp_DashboardGraphData");

                var result = await connection.QueryMultipleAsync(
                    "sp_DashboardGraphData",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                _logger.LogDebug("Reading result set 1 → GraphMaleFemaleChildren");
                var graphMaleFemaleChildren = result.ReadFirstOrDefault<GraphMaleFemaleChildren>();
                _logger.LogDebug("GraphMaleFemaleChildren {@Data}", graphMaleFemaleChildren);

                _logger.LogDebug("Reading result set 2 → GraphFemaleChildOtherAsHead");
                var graphFemaleChildOtherAsHead = result.ReadFirstOrDefault<GraphFemaleChildOtherAsHead>();
                _logger.LogDebug("GraphFemaleChildOtherAsHead {@Data}", graphFemaleChildOtherAsHead);

                _logger.LogDebug("Reading result set 3 → GraphRiskAssessment");
                var graphRiskAssessment = result.ReadFirstOrDefault<GraphRiskAssessment>();
                _logger.LogDebug("GraphRiskAssessment {@Data}", graphRiskAssessment);

                _logger.LogDebug("Reading result set 4 → GraphDistributionAssistance");
                var graphDistributionAssistance = result.ReadFirstOrDefault<GraphDistributionAssistance>();
                _logger.LogDebug("GraphDistributionAssistance {@Data}", graphDistributionAssistance);

                var dto = new DashboardGraphDto
                {
                    GraphMaleFemaleChildren = graphMaleFemaleChildren ?? new(),
                    GraphFemaleChildOtherAsHead = graphFemaleChildOtherAsHead ?? new(),
                    GraphRiskAssessment = graphRiskAssessment ?? new(),
                    GraphDistributionAssistance = graphDistributionAssistance ?? new()
                };

                _logger.LogInformation("Dashboard graph data loaded successfully");

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error loading dashboard graph data. TenantId={TenantId}, ProgramId={ProgramId}",
                    tenantId, programId);

                throw; //
            }
        }

    }
}
