using Azure.Core.Serialization;
using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Exceptions;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Office2013.Excel;
using DocumentFormat.OpenXml.Office2019.Word.Cid;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text.Json;

namespace BRaVe_Management_Backend.Services
{
    public class SqlActivitiesServices : IActivityService
    {
        private readonly string connectionString;
        private readonly ILogger<SqlActivitiesServices> _logger;

        public SqlActivitiesServices(ISecretProvider secretProvider, ILogger<SqlActivitiesServices> logger)
        {
            _logger = logger;
            connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection).Result;
        }

        public async Task AttachConsentAsync(int activityId, int consentId, int consentType, RequiredDto value)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = @"
        INSERT INTO tbl_ActivityConsent (ActivityId,ConsentType, ConsentId, TenantId, Required)
        SELECT @ActivityId,@ConsentType,@ConsentId, @TenantId, @Required
        WHERE NOT EXISTS (
            SELECT 1 
            FROM tbl_ActivityConsent
            WHERE ActivityId = @ActivityId 
                AND TenantId = @TenantId
              AND ConsentId = @ConsentId
        );";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ActivityId", activityId);
                cmd.Parameters.AddWithValue("@ConsentId", consentId);
                cmd.Parameters.AddWithValue("@ConsentType", consentType);
                cmd.Parameters.AddWithValue("@TenantId", value.TenantId);
                cmd.Parameters.AddWithValue("@Required", value.Required);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    _logger.LogInformation(
                        "Consent {ConsentId} attached to Activity {ActivityId} for Tenant {TenantId}. Required={Required}",
                        consentId, activityId, value.TenantId, value.Required
                    );
                }
                else
                {
                    _logger.LogWarning(
                        "Consent {ConsentId} is already attached to Activity {ActivityId} for Tenant {TenantId}. Insert skipped.",
                        consentId, activityId, value.TenantId
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error attaching Consent {ConsentId} to Activity {ActivityId} for Tenant {TenantId}",
                    consentId, activityId, value.TenantId
                );
                throw;
            }
        }


        public async Task AttachDataPointAsync(int activityId, int dataPointId, int datapointType, RequiredDto data)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                // Check if already exists
                var checkSql = @"
            SELECT COUNT(*) 
            FROM tbl_ActivityDataPoint 
            WHERE ActivityId = @ActivityId AND DataPointId = @DataPointId;";

                using (var checkCmd = new SqlCommand(checkSql, conn))
                {
                    checkCmd.Parameters.AddWithValue("@ActivityId", activityId);
                    checkCmd.Parameters.AddWithValue("@DataPointId", dataPointId);

                    var exists = (int)await checkCmd.ExecuteScalarAsync() > 0;
                    if (exists)
                        return; // Already exists, skip insert
                }

                // Insert
                var sql = @"
            INSERT INTO tbl_ActivityDataPoint (ActivityId, DataPointId,DatapointType, TenantId, Required)
            VALUES (@ActivityId, @DataPointId,@DatapointType, @TenantId, @Required)";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ActivityId", activityId);
                cmd.Parameters.AddWithValue("@DataPointId", dataPointId);
                cmd.Parameters.AddWithValue("@DatapointType", datapointType);
                cmd.Parameters.AddWithValue("@TenantId", data.TenantId);
                cmd.Parameters.AddWithValue("@Required", data.Required);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error attaching data point {DataPointId} to activity {ActivityId}", dataPointId, activityId);
            }
        }



        public async Task AttachDistributionsAsync(int activityId, int distributionId, int distributionType, int EnrollmentMode, bool PhotoConfirmation, bool BiometricVerification, RequiredDto value)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                // Check if already exists
                var checkSql = @"
            SELECT COUNT(*) 
            FROM tbl_ActivityDistributionTypes 
            WHERE ActivityId = @ActivityId
              AND DistributionId = @DistributionId
              AND DistributionType = @DistributionType
              AND TenantId = @TenantId;
        ";

                using (var checkCmd = new SqlCommand(checkSql, conn))
                {
                    checkCmd.Parameters.Add("@ActivityId", SqlDbType.Int).Value = activityId;
                    checkCmd.Parameters.Add("@DistributionId", SqlDbType.Int).Value = distributionId;
                    checkCmd.Parameters.Add("@DistributionType", SqlDbType.Int).Value = distributionType;
                    checkCmd.Parameters.Add("@TenantId", SqlDbType.Int).Value = value.TenantId;

                    var exists = (int)await checkCmd.ExecuteScalarAsync() > 0;

                    if (exists)
                        return; // Already exists
                }

                // Insert
                var insertSql = @"

            INSERT INTO tbl_ActivityDistributionTypes
                (ActivityId, DistributionId, DistributionType, TenantId,biometricconfirmation,photoconfirmation,DistributionMode )

            VALUES
                (@ActivityId, @DistributionId, @DistributionType, @TenantId,@biometricconfirmation,@photoconfirmation,@DistributionMode);
        ";

                using var cmd = new SqlCommand(insertSql, conn);

                cmd.Parameters.Add("@ActivityId", SqlDbType.Int).Value = activityId;
                cmd.Parameters.Add("@DistributionId", SqlDbType.Int).Value = distributionId;
                cmd.Parameters.Add("@DistributionType", SqlDbType.Int).Value = distributionType;
                cmd.Parameters.Add("@photoconfirmation", SqlDbType.Int).Value = PhotoConfirmation;
                cmd.Parameters.Add("@biometricconfirmation", SqlDbType.Int).Value = BiometricVerification;
                cmd.Parameters.Add("@DistributionMode", SqlDbType.Int).Value = EnrollmentMode;
                cmd.Parameters.Add("@TenantId", SqlDbType.Int).Value = value.TenantId;

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error attaching distribution {DistributionId} to activity {ActivityId}",
                    distributionId,
                    activityId
                );

                throw; // Optional: rethrow so API returns failure
            }
        }


        public async Task AttachEnumeratorsBulkAsync(int activityId, List<string> enumeratorCodes, int tenantId)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                // 1. Build DataTable (TVP source)
                var table = new DataTable();
                table.Columns.Add("EnumeratorCode", typeof(string));

                foreach (var code in enumeratorCodes)
                {
                    table.Rows.Add(code);
                }

                using var cmd = new SqlCommand("dbo.sp_AttachEnumeratorsBulk", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@ActivityId", activityId);
                cmd.Parameters.AddWithValue("@TenantId", tenantId);

                var tvpParam = cmd.Parameters.AddWithValue("@EnumeratorCodes", table);
                tvpParam.SqlDbType = SqlDbType.Structured;
                tvpParam.TypeName = "dbo.EnumeratorCodeList";

                await cmd.ExecuteNonQueryAsync();

                _logger.LogInformation(
                    "Bulk attached {Count} enumerators to Activity {ActivityId}",
                    enumeratorCodes.Count,
                    activityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed bulk attaching enumerators to Activity {ActivityId}",
                    activityId);

                throw;
            }
        }
        public enum PreferenceTypeEnum
        {
            INT = 1,
            NUMERIC = 2,
            BOOLEAN = 5,  //change from 3 to 5
            TEXT = 3 //4 to 3, to map with survey question types
        }

        public async Task AttachPreferenceAsync(int activityId, List<MissionPreferences> createdList)
        {
            if (createdList == null || createdList.Count == 0)
            {
                _logger.LogWarning("AttachPreference called with empty list.");
                return;
            }

            // Build a DataTable matching dbo.ActivityPreferenceTVP
            var tvp = new DataTable();
            tvp.Columns.Add("ActivityId", typeof(int));
            tvp.Columns.Add("PreferenceId", typeof(int));
            tvp.Columns.Add("TenantId", typeof(int));
            tvp.Columns.Add("PreferenceType", typeof(int));
            tvp.Columns.Add("DefaultValue", typeof(string)); // NVARCHAR(500)

            foreach (var pref in createdList)
            {
                var prefTypeEnum = pref.PreferenceType switch
                {
                    "INT" => PreferenceTypeEnum.INT,
                    "NUMERIC" => PreferenceTypeEnum.NUMERIC,
                    "BOOLEAN" => PreferenceTypeEnum.BOOLEAN,
                    "TEXT" => PreferenceTypeEnum.TEXT,
                    _ => PreferenceTypeEnum.TEXT
                };

                tvp.Rows.Add(
                    activityId,
                    pref.PreferenceId,
                    pref.TenantId,
                    (int)prefTypeEnum,
                    (object?)pref.DefaultValue ?? DBNull.Value
                );
            }

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                using var cmd = new SqlCommand("dbo.sp_AttachActivityPreferences_Batch", connection, transaction)
                {
                    CommandType = CommandType.StoredProcedure
                };

                var tvpParam = cmd.Parameters.AddWithValue("@Prefs", tvp);
                tvpParam.SqlDbType = SqlDbType.Structured;
                tvpParam.TypeName = "dbo.ActivityPreferenceTVP";

                // Optional: capture summary results (Inserted/Updated)
                using var reader = await cmd.ExecuteReaderAsync();
                int inserted = 0, updated = 0;

                if (await reader.ReadAsync())
                {
                    inserted = reader.GetInt32(reader.GetOrdinal("Inserted"));
                    updated = reader.GetInt32(reader.GetOrdinal("Updated"));
                }
                reader.Close();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Batch attach preferences for Activity {ActivityId} complete. Inserted={Inserted}, Updated={Updated}, Total={Total}",
                    activityId, inserted, updated, createdList.Count
                );
            }
            catch (SqlException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "SQL error during batch attach for Activity {ActivityId}", activityId);
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error during batch attach for Activity {ActivityId}", activityId);
                throw;
            }
        }


        public async Task AttachSurveyAsync(int activityId, int surveyId, RequiredDto value)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = @"
        INSERT INTO tbl_ActivitySurveys (ActivityId, SurveyId, TenantId, IsRequired)
        SELECT @ActivityId, @SurveyId, @TenantId, @IsRequired
        WHERE NOT EXISTS (
            SELECT 1 
            FROM tbl_ActivitySurveys
            WHERE ActivityId = @ActivityId AND SurveyId = @SurveyId
        );";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ActivityId", activityId);
                cmd.Parameters.AddWithValue("@SurveyId", surveyId);
                cmd.Parameters.AddWithValue("@TenantId", value.TenantId);
                cmd.Parameters.AddWithValue("@IsRequired", value.Required);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    _logger.LogInformation(
                        "Survey {SurveyId} attached to Activity {ActivityId} for Tenant {TenantId}. Required={Required}",
                        surveyId, activityId, value.TenantId, value.Required
                    );
                }
                else
                {
                    _logger.LogWarning(
                        "Survey {SurveyId} is already attached to Activity {ActivityId} for Tenant {TenantId}. Insert skipped.",
                        surveyId, activityId, value.TenantId
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error attaching Survey {SurveyId} to Activity {ActivityId} for Tenant {TenantId}",
                    surveyId, activityId, value.TenantId
                );
                throw;
            }
        }

        public async Task CreateActivity(RegistrationActivity newActivity)
        {
            try
            {
                await using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                await using var cmd = new SqlCommand("sp_CreateRegistrationActivity", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                string jsonAdminArea = null;

                if (newActivity.AdminArea != null)
                    jsonAdminArea = JsonSerializer.Serialize(newActivity.AdminArea);

                // Input parameters
                cmd.Parameters.AddWithValue("@ProgramId", newActivity.ProgramId);
                cmd.Parameters.AddWithValue("@TenantId", newActivity.TenantId);
                cmd.Parameters.AddWithValue("@StatusId", newActivity.StatusId);
                cmd.Parameters.AddWithValue("@Title", newActivity.Title ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Description", newActivity.Description ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@StartDate", newActivity.StartDate);
                cmd.Parameters.AddWithValue("@EndDate", newActivity.EndDate);
                cmd.Parameters.AddWithValue("@Longitude", newActivity.Longitude);
                cmd.Parameters.AddWithValue("@Latitude", newActivity.Latitude);
                cmd.Parameters.AddWithValue("@Deleted", newActivity.Deleted);
                cmd.Parameters.AddWithValue("@EnumeratorRestricted", newActivity.RestrictToEnumerator);
                cmd.Parameters.AddWithValue("@AllowRegistration", newActivity.AllowRegistration);
                cmd.Parameters.AddWithValue("@AllowBiometricRecordCheck", newActivity.AllowBiometricRecordCheck);
                cmd.Parameters.AddWithValue("@AdminAreas", jsonAdminArea ?? (object)DBNull.Value);

                cmd.Parameters.AddWithValue("@CreatedBy", newActivity.CreatedBy ?? (object)DBNull.Value);

                // Output parameters
                var activityIdParam = new SqlParameter("@ActivityId", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                var activityCodeParam = new SqlParameter("@ActivityCode", SqlDbType.NVarChar, 50)
                {
                    Direction = ParameterDirection.Output
                };

                cmd.Parameters.Add(activityIdParam);
                cmd.Parameters.Add(activityCodeParam);

                await cmd.ExecuteNonQueryAsync();

                // Set returned values
                newActivity.ActivityId = (int)activityIdParam.Value;
                newActivity.ActivityCode = (string)activityCodeParam.Value;
            }
            catch (Exception ex)
            {

            }

        }

        public async Task DetachDataPointAsync(int activityId, int dataPointId)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = @"
            DELETE FROM tbl_ActivityDatapoint 
            WHERE ActivityId = @ActivityId 
              AND DataPointId = @DataPointId";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ActivityId", activityId);
                cmd.Parameters.AddWithValue("@DataPointId", dataPointId);

                var rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    _logger.LogInformation(
                        "Successfully detached DataPoint {DataPointId} from Activity {ActivityId}. Rows affected: {Rows}",
                        dataPointId, activityId, rowsAffected);
                }
                else
                {
                    _logger.LogWarning(
                        "No matching record found to detach DataPoint {DataPointId} from Activity {ActivityId}",
                        dataPointId, activityId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while detaching DataPoint {DataPointId} from Activity {ActivityId}",
                    dataPointId, activityId);

                throw; // Optional: rethrow if the controller needs to handle it
            }
        }


        public async Task DetachDistributionsAsync(int activityId, int distributionId)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = @"
            DELETE FROM tbl_ActivityDistributionTypes
            WHERE ActivityId = @ActivityId
              AND DistributionId = @DistributionId";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ActivityId", activityId);
                cmd.Parameters.AddWithValue("@DistributionId", distributionId);

                var rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    _logger.LogInformation(
                        "Successfully detached distributor {DistributorId} from activity {ActivityId}. Rows affected: {Rows}",
                        distributionId, activityId, rowsAffected);
                }
                else
                {
                    _logger.LogWarning(
                        "No distribution found to detach for distributor {DistributorId} and activity {ActivityId}",
                        distributionId, activityId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error detaching distributor {DistributorId} from activity {ActivityId}",
                    distributionId, activityId);

                throw; // optional depending on how your controller handles errors
            }
        }


        public async Task DetachSurveyAsync(int activityId, int surveyId)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = @"
            DELETE FROM tbl_ActivitySurveys
            WHERE ActivityId = @ActivityId
              AND SurveyId = @SurveyId";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ActivityId", activityId);
                cmd.Parameters.AddWithValue("@SurveyId", surveyId);

                var rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    _logger.LogInformation(
                        "Successfully detached survey {SurveyId} from activity {ActivityId}. Rows affected: {Rows}",
                        surveyId, activityId, rowsAffected);
                }
                else
                {
                    _logger.LogWarning(
                        "No survey found to detach for SurveyId {SurveyId} and ActivityId {ActivityId}",
                        surveyId, activityId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error detaching survey {SurveyId} from activity {ActivityId}",
                    surveyId, activityId);

                throw; // optional based on your architecture
            }
        }


        public async Task<RegistrationActivity> GetActivityById(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid activity id.", nameof(id));

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = @"
                    SELECT *
                    FROM tbl_RegistrationActivities
                    WHERE ActivityId = @ActivityId";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ActivityId", id);

                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                    return null; // Activity not found

                var activity = new RegistrationActivity
                {
                    ActivityId = reader.GetInt32(reader.GetOrdinal("ActivityId")),
                    ProgramId = reader.GetInt32(reader.GetOrdinal("ProgramId")),
                    TenantId = reader.GetInt32(reader.GetOrdinal("TenantId")),
                    StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),

                    Title = reader.IsDBNull(reader.GetOrdinal("Title"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("Title")),

                    Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("Description")),

                    ActivityCode = reader.IsDBNull(reader.GetOrdinal("ActivityCode"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("ActivityCode")),

                    // Nullable boolean
                    RestrictToEnumerator = reader.IsDBNull(reader.GetOrdinal("EnumeratorRestricted"))
                            ? (bool?)null
                            : reader.GetBoolean(reader.GetOrdinal("EnumeratorRestricted")),

                    // Nullable decimal Longitude / Latitude
                    Longitude = reader.IsDBNull(reader.GetOrdinal("Longitude"))
                                                    ? 0m
                            : reader.GetDecimal(reader.GetOrdinal("Longitude")),

                    Latitude = reader.IsDBNull(reader.GetOrdinal("Latitude"))
                         ? 0m
                         : reader.GetDecimal(reader.GetOrdinal("Latitude")),


                    // Nullable dates
                    StartDate = reader.IsDBNull(reader.GetOrdinal("StartDate"))
                            ? (DateTime?)null
                            : reader.GetDateTime(reader.GetOrdinal("StartDate")),

                    EndDate = reader.IsDBNull(reader.GetOrdinal("EndDate"))
                            ? (DateTime?)null
                            : reader.GetDateTime(reader.GetOrdinal("EndDate")),

                    IsActive = reader.IsDBNull(reader.GetOrdinal("IsActive"))
                            ? false
                            : reader.GetBoolean(reader.GetOrdinal("IsActive")),

                    AllowRegistration = reader.IsDBNull(reader.GetOrdinal("AllowRegistration"))
                            ? false
                            : reader.GetBoolean(reader.GetOrdinal("AllowRegistration")),

                    AllowBiometricRecordCheck = reader.IsDBNull(reader.GetOrdinal("AllowBiometricRecordCheck"))
                            ? false
                            : reader.GetBoolean(reader.GetOrdinal("AllowBiometricRecordCheck")),

                    JsonAdminAreas = reader.IsDBNull(reader.GetOrdinal("AdminAreas"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("AdminAreas"))
                };

                if (!string.IsNullOrEmpty(activity.JsonAdminAreas))
                {
                    try
                    {
                        activity.AdminArea = JsonSerializer.Deserialize<AdminArea>(activity.JsonAdminAreas);
                    }
                    catch (Exception ex)
                    {

                    }
                }

                return activity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching activity with Id {ActivityId}", id);
                throw;
            }
        }


        public async Task<IEnumerable<RegistrationActivity>> GetAllActivities(int programId)
        {
            _logger.LogInformation("Fetching all Activities for program {ProgramId}", programId);

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = @"SELECT * 
                    FROM tbl_RegistrationActivities 
                    WHERE ProgramId = @ProgramId";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ProgramId", programId);

                var activities = new List<RegistrationActivity>();

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var act = new RegistrationActivity
                    {
                        ActivityId = reader.GetInt32(reader.GetOrdinal("ActivityId")),
                        ProgramId = reader.GetInt32(reader.GetOrdinal("ProgramId")),
                        TenantId = reader.GetInt32(reader.GetOrdinal("TenantId")),
                        StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),

                        Title = reader.IsDBNull(reader.GetOrdinal("Title"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("Title")),

                        Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("Description")),

                        ActivityCode = reader.IsDBNull(reader.GetOrdinal("ActivityCode"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("ActivityCode")),

                        // Nullable boolean
                        RestrictToEnumerator = reader.IsDBNull(reader.GetOrdinal("EnumeratorRestricted"))
                            ? (bool?)null
                            : reader.GetBoolean(reader.GetOrdinal("EnumeratorRestricted")),

                        // Nullable decimal Longitude / Latitude
                        Longitude = reader.IsDBNull(reader.GetOrdinal("Longitude"))
                                                    ? 0m
                            : reader.GetDecimal(reader.GetOrdinal("Longitude")),

                        Latitude = reader.IsDBNull(reader.GetOrdinal("Latitude"))
                         ? 0m
                         : reader.GetDecimal(reader.GetOrdinal("Latitude")),


                        // Nullable dates
                        StartDate = reader.IsDBNull(reader.GetOrdinal("StartDate"))
                            ? (DateTime?)null
                            : reader.GetDateTime(reader.GetOrdinal("StartDate")),

                        EndDate = reader.IsDBNull(reader.GetOrdinal("EndDate"))
                            ? (DateTime?)null
                            : reader.GetDateTime(reader.GetOrdinal("EndDate")),

                        IsActive = reader.IsDBNull(reader.GetOrdinal("IsActive"))
                            ? false
                            : reader.GetBoolean(reader.GetOrdinal("IsActive")),

                        AllowRegistration = reader.IsDBNull(reader.GetOrdinal("AllowRegistration"))
                            ? false
                            : reader.GetBoolean(reader.GetOrdinal("AllowRegistration")),

                        AllowBiometricRecordCheck = reader.IsDBNull(reader.GetOrdinal("AllowBiometricRecordCheck"))
                            ? false
                            : reader.GetBoolean(reader.GetOrdinal("AllowBiometricRecordCheck")),

                        JsonAdminAreas = reader.IsDBNull(reader.GetOrdinal("AdminAreas"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("AdminAreas"))


                    };

                    if (!string.IsNullOrEmpty(act.JsonAdminAreas))
                    {
                        try
                        {
                            act.AdminArea = JsonSerializer.Deserialize<AdminArea>(act.JsonAdminAreas);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error while deserializing AdminArea for entry Registration activity Id :{act.ActivityCode} in Program {programId}.");
                            act.AdminArea = null;
                        }

                    }

                    activities.Add(act);
                }

                _logger.LogInformation("Fetched {Count} Registration activities for Program {ProgramId}.",
                    activities.Count, programId);

                return activities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Registration activities for Program {ProgramId}.", programId);
                throw;
            }
        }

        public async Task<IEnumerable<ActivityPreference>> GetAttachedPreferences(int activityId, int TenantId)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = @"SELECT a.*, p.PreferenceName, p.Description   FROM tbl_ActivityPreference a  INNER JOIN tbl_Preferences p ON a.PreferenceId = p.Id
                    WHERE a.ActivityId = @ActivityId";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ActivityId", activityId);

                var result = new List<ActivityPreference>();

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var dp = new ActivityPreference
                    {
                        ActivityId = reader.GetInt32(reader.GetOrdinal("ActivityId")),
                        PreferenceId = reader.GetInt32(reader.GetOrdinal("PreferenceId")),
                        TenantId = reader.GetInt32(reader.GetOrdinal("TenantId")),
                        PreferenceType = reader.GetInt32(reader.GetOrdinal("PreferenceType")),
                        PreferenceName = reader.GetString(reader.GetOrdinal("PreferenceName")),
                        DefaultValue = reader.GetString(reader.GetOrdinal("DefaultValue"))

                    };

                    result.Add(dp);
                }

                _logger.LogInformation("Fetched {Count} Consent for Activity {ActivityId}", result.Count, activityId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching datapoints for Activity {ActivityId}", activityId);
                throw;
            }
        }

        public async Task<IEnumerable<ActivityConsent>> GetConsentForActivity(int tenantId,int activityId, string languageCode)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("dbo.sp_GetConsentForActivity", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@TenantId", tenantId);
                cmd.Parameters.AddWithValue("@ActivityId", activityId);
                cmd.Parameters.AddWithValue("@LanguageCode", languageCode);

                _logger.LogInformation(
                    "Fetching consents for ActivityId={ActivityId}, LanguageCode={LanguageCode}",
                    activityId,
                    languageCode);

                var result = new List<ActivityConsent>();

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add(new ActivityConsent
                    {
                        ActivityId = reader.GetInt32(reader.GetOrdinal("ActivityId")),
                        ConsentId = reader.GetInt32(reader.GetOrdinal("ConsentId")),
                        ConsentType = reader.GetInt32(reader.GetOrdinal("ConsentType")),
                        TenantId = reader.GetInt32(reader.GetOrdinal("TenantId")),
                        Required = reader.GetBoolean(reader.GetOrdinal("Required")),
                        Title = reader["Title"]?.ToString() ?? string.Empty,
                        Description = reader["Description"]?.ToString() ?? string.Empty
                    });
                }

                _logger.LogInformation(
                    "Fetched {Count} consents for ActivityId={ActivityId}, LanguageCode={LanguageCode}",
                    result.Count,
                    activityId,
                    languageCode);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error fetching consents for ActivityId={ActivityId}, LanguageCode={LanguageCode}",
                    activityId,
                    languageCode);

                throw;
            }
        }
        public async Task<IEnumerable<ActivityDataPoint>> GetDataPointsForActivity(int activityId)
        {
            _logger.LogInformation("Fetching datapoints linked to Activity {ActivityId}", activityId);

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = @"SELECT ActivityId, DataPointId,DatapointType, TenantId, Required 
                    FROM tbl_ActivityDataPoint 
                    WHERE ActivityId = @ActivityId";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ActivityId", activityId);

                var result = new List<ActivityDataPoint>();

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var dp = new ActivityDataPoint
                    {
                        ActivityId = reader.GetInt32(reader.GetOrdinal("ActivityId")),
                        DataPointId = reader.GetInt32(reader.GetOrdinal("DataPointId")),
                        DatapointType = reader.GetInt32(reader.GetOrdinal("DatapointType")),
                        TenantId = reader.GetInt32(reader.GetOrdinal("TenantId")),
                        Required = reader.GetBoolean(reader.GetOrdinal("Required"))
                    };

                    result.Add(dp);
                }

                _logger.LogInformation("Fetched {Count} datapoints for Activity {ActivityId}", result.Count, activityId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching datapoints for Activity {ActivityId}", activityId);
                throw;
            }
        }


        public async Task<IEnumerable<ActivityDistributions>> GetDistributionsForActivity(int activityId)
        {
            _logger.LogInformation(
                "Fetching distributions linked to Activity {ActivityId}",
                activityId
            );

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = @"
                        SELECT 
                            ad.ActivityId,
                            ad.DistributionId,
                            ad.TenantId,
                            ad.DistributionType,
                            d.Title AS Name,
                            d.Description AS Note,
                            ad.photoconfirmation as Photo,
                            ad.biometricconfirmation as Biometric,
                               Case when DistributionMode=1 then 'Online' 
                                    when DistributionMode=2 then 'Offline' else 'Hybrid' end AS Mode
                        FROM tbl_ActivityDistributionTypes ad
                        INNER JOIN tbl_DistributionTypes d ON ad.DistributionId = d.Id AND ad.TenantId = d.TenantId
                        WHERE ad.ActivityId = @ActivityId";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ActivityId", activityId);

                var result = new List<ActivityDistributions>();

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var distribution = new ActivityDistributions
                    {
                        ActivityId = reader.GetInt32(reader.GetOrdinal("ActivityId")),
                        DistributionId = reader.GetInt32(reader.GetOrdinal("DistributionId")),
                        TenantId = reader.GetInt32(reader.GetOrdinal("TenantId")),
                        DistributionType = reader.GetInt32(reader.GetOrdinal("DistributionType")),

                        //Code = reader.IsDBNull(reader.GetOrdinal("Code"))
                        //    ? null
                        //    : reader.GetString(reader.GetOrdinal("Code")),

                        Name = reader.IsDBNull(reader.GetOrdinal("Name"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("Name")),

                        note = reader.IsDBNull(reader.GetOrdinal("Note"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("Note")),
                        Photo = reader.IsDBNull(reader.GetOrdinal("Photo"))
                                    ? (bool?)null
                                    : reader.GetBoolean(reader.GetOrdinal("Photo")),
                        Biometric = reader.IsDBNull(reader.GetOrdinal("Biometric"))
                                    ? (bool?)null
                                    : reader.GetBoolean(reader.GetOrdinal("Biometric")),
                        Mode = reader.IsDBNull(reader.GetOrdinal("Mode"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("Mode"))

                    };

                    result.Add(distribution);
                }

                _logger.LogInformation(
                    "Fetched {Count} distributions for Activity {ActivityId}",
                    result.Count,
                    activityId
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error fetching distributions for Activity {ActivityId}",
                    activityId
                );
                throw;
            }
        }

        public async Task<IEnumerable<ActivityEnumerator>> GetEnumeratorsForActivity(int activityId)
        {
            _logger.LogInformation(
                "Fetching enumerators linked to Activity {ActivityId}",
                activityId
            );

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = @"
                 SELECT 
                        ae.ActivityId,
                        ae.EnumeratorCode,
                        e.FullName,
                         e.IsActive
                    FROM tbl_ActivityEnumerator ae
                    INNER JOIN tbl_Enumerators e 
                        ON ae.EnumeratorCode = e.EnumeratorCode and ae.TenantId =e.TenantId
                    WHERE ae.ActivityId = @ActivityId
";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ActivityId", activityId);

                var result = new List<ActivityEnumerator>();

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var enumerator = new ActivityEnumerator
                    {
                        ActivityId = reader.GetInt32(reader.GetOrdinal("ActivityId")),
                        EnumeratorCode = reader.GetString(reader.GetOrdinal("EnumeratorCode")),
                        FullName = reader.IsDBNull(reader.GetOrdinal("FullName"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("FullName")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                    };

                    result.Add(enumerator);
                }

                _logger.LogInformation(
                    "Fetched {Count} enumerators for Activity {ActivityId}",
                    result.Count,
                    activityId
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error fetching enumerators for Activity {ActivityId}",
                    activityId
                );
                throw;
            }
        }


        public async Task<IEnumerable<ActivitySurveys>> GetSurveysForActivity(int activityId)
        {
            var results = new List<ActivitySurveys>();

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = @"
                        SELECT t.*,s.TiTle , s.SurveyType
                        FROM tbl_ActivitySurveys t inner join tbl_Surveys s on t.SurveyId= s.SurveyId
                        WHERE ActivityId = @ActivityId";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ActivityId", activityId);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(new ActivitySurveys
                    {
                        ActivityId = reader.GetInt32(reader.GetOrdinal("ActivityId")),
                        SurveyId = reader.GetInt32(reader.GetOrdinal("SurveyId")),
                        TenantId = reader.GetInt32(reader.GetOrdinal("TenantId")),
                        IsRequired = reader.GetBoolean(reader.GetOrdinal("IsRequired")),
                        Title = reader.GetString(reader.GetOrdinal("Title")),
                        SurveyType = reader.GetInt32(reader.GetOrdinal("SurveyType"))
                    });
                }
            }
            catch (Exception ex)
            {
                // TODO: log the error
                throw;
            }

            return results;
        }


        public async Task RemoveAttachedConsentAsync(int activityId, int consentId)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = @"
            DELETE FROM tbl_ActivityConsent
            WHERE ActivityId = @ActivityId
              AND ConsentId = @ConsentId";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ActivityId", activityId);
                cmd.Parameters.AddWithValue("@ConsentId", consentId);

                var rows = await cmd.ExecuteNonQueryAsync();

                if (rows > 0)
                {
                    _logger.LogInformation(
                        "Successfully detached consent {ConsentId} from activity {ActivityId}. Rows affected: {Rows}",
                        consentId, activityId, rows);
                }
                else
                {
                    _logger.LogWarning(
                        "No attached consent found for ConsentId {ConsentId} and ActivityId {ActivityId}. Nothing removed.",
                        consentId, activityId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error detaching consent {ConsentId} from activity {ActivityId}",
                    consentId, activityId);

                throw; // optional depending on your architecture
            }
        }


        public async Task RemoveAttachedSurveyAsync(int activityId, int surveyId)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = @"
                        DELETE FROM tbl_ActivitySurveys
                        WHERE ActivityId = @ActivityId
                          AND SurveyId = @SurveyId";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ActivityId", activityId);
                cmd.Parameters.AddWithValue("@SurveyId", surveyId);

                var rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    _logger.LogInformation(
                        "Successfully detached survey {SurveyId} from activity {ActivityId}. Rows affected: {Rows}",
                        surveyId, activityId, rowsAffected);
                }
                else
                {
                    _logger.LogWarning(
                        "No survey found to detach for SurveyId {SurveyId} and ActivityId {ActivityId}",
                        surveyId, activityId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error detaching survey {SurveyId} from activity {ActivityId}",
                    surveyId, activityId);

                throw; // optional based on your architecture
            }
        }

        public Task RemoveDistributionsAsync(int activityId, int distributorId)
        {
            throw new NotImplementedException();
        }
        public async Task RemoveEnumeratorAsync(int activityId, string enumeratorCode)
        {
            _logger.LogInformation(
                "Removing Enumerator {EnumeratorId} from Activity {ActivityId}",
                enumeratorCode,
                activityId
            );

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = @"
           DELETE FROM tbl_ActivityEnumerator
            WHERE ActivityId = @ActivityId
              AND EnumeratorCode = @EnumeratorCode";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ActivityId", activityId);
                cmd.Parameters.AddWithValue("@EnumeratorCode", enumeratorCode);

                var rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected == 0)
                {
                    _logger.LogWarning(
                        "No Enumerator found to remove for Activity {ActivityId} and Enumerator {EnumeratorId}",
                        activityId,
                        enumeratorCode
                    );
                }
                else
                {
                    _logger.LogInformation(
                        "Enumerator {EnumeratorId} successfully removed from Activity {ActivityId}",
                        enumeratorCode,
                        activityId
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error removing Enumerator {EnumeratorId} from Activity {ActivityId}",
                    enumeratorCode,
                    activityId
                );
                throw;
            }
        }

        public async Task RemovePreferenceAsync(int activityId, int preferenceId)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = @"
            DELETE FROM tbl_ActivityPreference
            WHERE ActivityId = @ActivityId
              AND PreferenceId = @PreferenceId";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ActivityId", activityId);
                cmd.Parameters.AddWithValue("@PreferenceId", preferenceId);

                var rows = await cmd.ExecuteNonQueryAsync();

                if (rows > 0)
                {
                    _logger.LogInformation(
                        "Successfully removed preference {PreferenceId} from activity {ActivityId}. Rows affected: {Rows}",
                        preferenceId, activityId, rows);
                }
                else
                {
                    _logger.LogWarning(
                        "No preference was found for PreferenceId {PreferenceId} on ActivityId {ActivityId}. Nothing removed.",
                        preferenceId, activityId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error removing preference {PreferenceId} from activity {ActivityId}",
                    preferenceId, activityId);

                throw; // Re-throw if you want the API layer to handle it
            }
        }


        public async Task UpdateActivity(RegistrationActivity updatedActivity)
        {
            if (updatedActivity == null)
                throw new ArgumentNullException(nameof(updatedActivity));

            try
            {
                await using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = @"
                        UPDATE tbl_RegistrationActivities
                        SET
                            Title = @Title,
                            Description = @Description,
                            StartDate = @StartDate,
                            EndDate = @EndDate,
                            Longitude = @Longitude,
                            Latitude = @Latitude,
                            EnumeratorRestricted = @EnumeratorRestricted,
                            AllowRegistration = @AllowRegistration,
                            AllowBiometricRecordCheck = @AllowBiometricRecordCheck,
                            AdminAreas = @AdminAreas,
                            StatusId = @StatusId,
                            UpdatedByUserId = @UpdatedByUserId,
                            UpdatedOn = GETDATE()
                        WHERE ActivityId = @ActivityId";

                string jsonAdminArea = null;

                if (updatedActivity.AdminArea != null)
                    jsonAdminArea = JsonSerializer.Serialize(updatedActivity.AdminArea);

                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ActivityId", updatedActivity.ActivityId);
                cmd.Parameters.AddWithValue("@Title", updatedActivity.Title);
                cmd.Parameters.AddWithValue("@Description", updatedActivity.Description ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@StartDate", updatedActivity.StartDate ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@EndDate", updatedActivity.EndDate ?? (object)DBNull.Value);

                cmd.Parameters.AddWithValue("@Longitude", updatedActivity.Longitude);
                cmd.Parameters.AddWithValue("@Latitude", updatedActivity.Latitude);
                cmd.Parameters.AddWithValue("@EnumeratorRestricted", updatedActivity.RestrictToEnumerator);
                cmd.Parameters.AddWithValue("@AllowRegistration", updatedActivity.AllowRegistration);
                cmd.Parameters.AddWithValue("@AllowBiometricRecordCheck", updatedActivity.AllowBiometricRecordCheck);
                cmd.Parameters.AddWithValue("@AdminAreas", jsonAdminArea ?? (object)DBNull.Value);


                cmd.Parameters.AddWithValue("@StatusId", updatedActivity.StatusId);
                cmd.Parameters.AddWithValue("@UpdatedByUserId", updatedActivity.UpdatedByUserId ?? (object)DBNull.Value);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating activity {ActivityId}", updatedActivity.ActivityId);
                throw;
            }
        }

        public async Task Validate(int ActivityId, int TenantId, string UpdatedByUserId)
        {
            try
            {
                await using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = @"
                        UPDATE tbl_RegistrationActivities
                        SET IsActive = 1, UpdatedOn= GETUTCDATE()
                        WHERE ISNULL(IsActive, 0) = 0 AND TenantId=@TenantId AND ActivityId = @ActivityId";


                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ActivityId", ActivityId);
                cmd.Parameters.AddWithValue("@TenantId", TenantId);
                cmd.Parameters.AddWithValue("@UpdatedByUserId", UpdatedByUserId);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating activity {ActivityId}", ActivityId);
                throw;
            }
        }

        public async Task Invalidate(int ActivityId, int TenantId, string UpdatedByUserId)
        {
            try
            {
                await using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = @"
                        UPDATE tbl_RegistrationActivities
                        SET IsActive = 0, UpdatedOn= GETUTCDATE()
                        WHERE ISNULL(IsActive, 0) = 1 AND TenantId=@TenantId AND ActivityId = @ActivityId";


                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ActivityId", ActivityId);
                cmd.Parameters.AddWithValue("@TenantId", TenantId);
                cmd.Parameters.AddWithValue("@UpdatedByUserId", UpdatedByUserId);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating activity {ActivityId}", ActivityId);
                throw;
            }
        }
    }

}

