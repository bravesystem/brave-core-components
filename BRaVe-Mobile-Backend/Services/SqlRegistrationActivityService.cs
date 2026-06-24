using BRaVe_Mobile_Backend.Exceptions;
using BRaVe_Mobile_Backend.Helpers;
using BRaVe_Mobile_Backend.Interfaces;
using BRaVe_Mobile_Backend.Models;
using BRaVe_Mobile_Backend.Models.data_payload;
using Dapper;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Text.Json;

namespace BRaVe_Mobile_Backend.Services
{
    public class SqlRegistrationActivityService : IRegistrationActivityService
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlRegistrationActivityService> _logger;

        public SqlRegistrationActivityService(ISecretProvider secretProvider,
                                              ILogger<SqlRegistrationActivityService> logger)
        {
            //_connectionString = config.GetConnectionString("DefaultConnection");
            _connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection);
            _logger = logger;
        }

        public async Task<EnrollmentResponse> fetchEnrollment(string activityId, int tenantId, string deviceId, EnrollmentRequest request)
        {
            if (string.IsNullOrWhiteSpace(activityId))
                throw new ActivityErrors.NullValue();

            try
            {
                using var connection = new SqlConnection(_connectionString);

                var parameters = new DynamicParameters();
                parameters.Add("@ActivityCode", activityId);
                parameters.Add("@TenantId", tenantId);
                parameters.Add("@DeviceId", deviceId);
                parameters.Add("@distributionId", request.distributionId);
                parameters.Add("@HOHID", request.beneficiaryId);

                using var multi = await connection.QueryMultipleAsync(
                    "sp_GetEnrollmentByBeneficiaryHohid",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                // 1. Household
                var family = multi.ReadFirstOrDefault<FamilyProfile>();
                if (family == null)
                    throw new EnrollmentErrors.NotFound();

                // 18.1 Distribution Kits
                var members = multi.Read<MemberInfo>().ToList();
                foreach (var m in members)
                    family.members = members.Where(i => i.householdId == family.hohid).ToList();

                return new EnrollmentResponse
                {
                    beneficiary = family
                };

            }
            catch (SqlException ex) when (ex.Number == 50020)
            {
                _logger.LogError(ex,
                        "Unauthorized. ActivityCode={ActivityId}, TenantId={TenantId}, Distribution={DistributionId}, Beneficiary={BeneficiaryId}",
                        activityId, tenantId, request.distributionId, request.beneficiaryId);
                throw;
            }
            catch (SqlException ex) when (ex.Number == 50010)
            {
                _logger.LogError(ex,
                        "Beneficiary not enrolled. ActivityCode={ActivityId}, TenantId={TenantId}, Distribution={DistributionId}, Beneficiary={BeneficiaryId}",
                        activityId, tenantId, request.distributionId, request.beneficiaryId);
                throw;
            }
            catch (Exception ex) {
                _logger.LogError(ex,
                        "Unexpected error fetching enrollment data. ActivityCode={ActivityId}, TenantId={TenantId}, Distribution={DistributionId}, Beneficiary={BeneficiaryId}",
                        activityId, tenantId, request.distributionId, request.beneficiaryId);
                throw;
            }
        }

        public async Task<EnrollmentResponseBulk> fetchEnrollmentBulk(string activityId, int tenantId, string enumerator, string deviceId, EnrollmentRequestBulk request)
        {
            if (string.IsNullOrWhiteSpace(activityId))
                throw new ActivityErrors.NullValue();

            EnrollmentResponseBulk response = new EnrollmentResponseBulk();

            try 
            {
                using var connection = new SqlConnection(_connectionString);

                var parameters = new DynamicParameters();
                parameters.Add("@ActivityCode", activityId);
                parameters.Add("@TenantId", tenantId);
                parameters.Add("@Enumerator", enumerator);
                parameters.Add("@DeviceId", deviceId);
                parameters.Add("@distributionId", request.distributionId);
                parameters.Add("@ParticipationCode", request.participationCode);

                using var multi = await connection.QueryMultipleAsync(
                    "sp_GetEnrollmentsByParticipationCode",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                // 1. Household
                var family = multi.ReadFirstOrDefault<FamilyProfile>();
                if (family == null)
                    throw new EnrollmentErrors.NotFound();

                // 18.1 Distribution Kits
                var members = multi.Read<MemberInfo>().ToList();
                foreach (var m in members)
                    family.members = members.Where(i => i.householdId == family.hohid).ToList();

                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                        "Unexpected error fetching enrollment data. ActivityCode={ActivityId}, TenantId={TenantId}, Distribution={DistributionId}, ParticipationCode={ParticipationCode}",
                        activityId, tenantId, request.distributionId, request.participationCode);
                throw;
            }

            return response;

        }

        public async Task<RegistrationActivity> GetActivity(string deviceId,string activityId, int tenantId, string lang)
        {
            if (string.IsNullOrWhiteSpace(activityId))
                throw new ActivityErrors.NullValue();

            try
            {
                using var connection = new SqlConnection(_connectionString);

                var parameters = new DynamicParameters();
                parameters.Add("@DeviceId",  deviceId);
                parameters.Add("@ActivityCode", activityId);
                parameters.Add("@TenantId", tenantId);
                parameters.Add("@Language", lang);

                using var multi = await connection.QueryMultipleAsync(
                    "sp_GetRegistrationActivityV4",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                // 1. Main Activity
                var activity = multi.ReadFirstOrDefault<RegistrationActivity>();
                if (activity == null)
                    throw new ActivityErrors.NotFound();

                // 2. Admin Levels
                activity.AdminLevels = multi.Read<AdminLevel>().ToList();

                // 3. Admin Locations
                activity.AdminLocations = multi.Read<AdminLocation>().ToList();

                // 4. Lookups
                var lookups = multi.Read<CustomLookup>().ToList();
                var lookupValues = multi.Read<CustomKeyPair>().ToList(); // assuming you have CustomKeyPair for values
                foreach (var l in lookups)
                {
                    l.Values = lookupValues.Where(v => v.LookupId == l.Id).ToList();
                }
                activity.Lookups = lookups;

                // 5. Datasets
                activity.Datasets = multi.Read<CustomDataset>().ToList();

                // 6. Dataset Columns
                /*var datasetColumns = multi.Read<DatasetColumn>().ToList();
                foreach (var ds in activity.Datasets)
                    ds.Columns = datasetColumns.Where(c => c.DatasetId == ds.Id).ToList();*/

                // 7. Surveys
                activity.Surveys = multi.Read<Survey>().ToList();

                // 8. Survey Questions
                var questions = multi.Read<Question>().ToList();
                foreach (var s in activity.Surveys)
                    s.Questions = questions.Where(q => q.SurveyCode == s.SurveyCode).ToList();

                // 9. Survey Translations
                var surveyTranslations = multi.Read<Translation>().ToList();
                foreach (var q in questions)
                    q.Texts = surveyTranslations.Where(t => t.QuestionId == q.Id && t.SurveyCode==q.SurveyCode ).ToList();

                // 10. Datapoints
                activity.Datapoints = multi.Read<Datapoint>().ToList();

                // 11. Datapoint Translations
                var dpTranslations = multi.Read<DataPointTranslation>().ToList();

                foreach (var dp in activity.Datapoints)
                    dp.Texts = dpTranslations.Where(t => t.DataPointId == dp.Id).Select(t=>new Translation() { 
                        Language=t.Language,Text=t.Text
                    }).ToList();

                // 12. Datapoint Bindings
                activity.DatapointBindings = multi.Read<DatapointBinding>().ToList();

                // 13. Survey Bindings
                activity.SurveyBindings = multi.Read<SurveyBinding>().ToList();

                // 14. Consents
                activity.Consents = multi.Read<Consent>().ToList();

                // 15. Consent Bindings
                activity.ConsentBindings = multi.Read<ConsentBinding>().ToList();

                // 16. Whitelist
                activity.Whitelist = multi.Read<string>().ToList();

                // 17. Preferences
                activity.Preferences = multi.Read<ActivityPreference>().ToList();

                //18. Distributions
                activity.Distributions = multi.Read<Distribution>().ToList();

                // 18.1 Distribution Kits
                var kits = multi.Read<Kit>().ToList();
                foreach (var d in activity.Distributions)
                    d.kits = kits.Where(k => k.DistributionId == d.DistributionId).ToList();

                // 18.2 Kit Items
                var items = multi.Read<DistrItem>().ToList();
                foreach (var k in kits)
                    k.items = items.Where(i => i.KitId == k.KitId).ToList();

                // 19. Distribution Bindings
                activity.DistributionBindings = multi.Read<DistributionBinding>().ToList();


                return activity;
            }
            catch (ActivityErrors.NullValue ex)
            {
                _logger.LogWarning(ex,
                    "Null or empty ActivityId was provided. ActivityCode={ActivityId}, TenantId={TenantId}",
                    activityId, tenantId);
                throw;
            }
            catch (ActivityErrors.NotFound ex)
            {
                _logger.LogWarning(ex,
                    "Activity not found. ActivityCode={ActivityId}, TenantId={TenantId}",
                    activityId, tenantId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error retrieving activity. ActivityCode={ActivityId}, TenantId={TenantId}",
                    activityId, tenantId);
                throw; // rethrow or wrap as database error if needed
            }
        }
        
        public async Task SaveAsync(RegistrationActivityStaging stagingRecord)
        {
            using var cn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("insert into tbl_RegistrationActivityStaging (DeviceId, TenantId, Activitycode, Type, Payload, BatchId) " +
                "values(@DeviceId, @TenantId, @Activitycode, @Type, @Payload, @BatchId);", cn);

            cmd.Parameters.Add("@DeviceId", SqlDbType.VarChar, 50).Value = stagingRecord.DeviceId;
            cmd.Parameters.Add("@TenantId", SqlDbType.Int).Value = stagingRecord.TenantId;
            cmd.Parameters.Add("@Activitycode", SqlDbType.VarChar, 50).Value = stagingRecord.ActivityCode;
            cmd.Parameters.Add("@Type", SqlDbType.Int).Value = (int)stagingRecord.Type;
            cmd.Parameters.Add("@Payload", SqlDbType.NVarChar, -1).Value = stagingRecord.Payload;
            cmd.Parameters.Add("@BatchId", SqlDbType.UniqueIdentifier).Value = stagingRecord.BatchId;

            await cn.OpenAsync();

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<BiometricVerificationResponse>> VerifyTemplates(int TenantId, Guid jobId, string activityId,  string DeviceId, int DistributionId, List<BiometricVerificationRequest> data)
        {
            List <BiometricVerificationResponse> responses = new List <BiometricVerificationResponse>();

            _logger.LogInformation($"Verification requests: {data.Count}");

            try
            {
                var dt = new DataTable();
                dt.Columns.AddRange(
                [
                    new DataColumn("uuid", typeof(Guid)),
                    new DataColumn("gender", typeof(int)),
                    new DataColumn("template", typeof(byte[])),
                    new DataColumn("createdByUserId", typeof(string)),
                    new DataColumn("createdOnMs", typeof(long)),
                ]);

                foreach (var b in data)
                {
                    dt.Rows.Add(b.uuid, b.gender, Convert.FromBase64String(b.template), b.createdByUserId, b.createdOnMs);
                }


                using var cn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("sp_VerifyTemplatesV2", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.Add(new SqlParameter("@TenantId", SqlDbType.Int) { Value = TenantId });
                cmd.Parameters.Add(new SqlParameter("@JobId", SqlDbType.UniqueIdentifier) { Value = jobId });
                cmd.Parameters.Add("@ActivityCode", SqlDbType.VarChar, 50).Value = activityId;
                cmd.Parameters.Add(new SqlParameter("@DeviceId", SqlDbType.VarChar, 50) { Value = DeviceId });
                cmd.Parameters.Add(new SqlParameter("@DistributionId", SqlDbType.Int) { Value = DistributionId });

                var tvpParam = new SqlParameter("@Templates", SqlDbType.Structured)
                {
                    TypeName = "dbo.TemplateVerification",
                    Value = dt
                };
                cmd.Parameters.Add(tvpParam);


                await cn.OpenAsync();

                // Helps attach members to the right FamilyProfile
                var familyByHouseholdUuid = new Dictionary<string, FamilyProfile>(StringComparer.OrdinalIgnoreCase);

                // Helps attach matched FamilyProfile to response
                var responseByUuid = new Dictionary<string, BiometricVerificationResponse>(StringComparer.OrdinalIgnoreCase);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    //fetch main table
                    while (await reader.ReadAsync())
                    {
                        Guid uuid = reader.GetGuid(reader.GetOrdinal("uuid"));

                        var resp = new BiometricVerificationResponse
                        {
                            uuid = uuid.ToString(),
                            is_processed = reader.GetBoolean(reader.GetOrdinal("isProcessed")),
                            match_found = reader.GetInt32(reader.GetOrdinal("match_found")) == 1,
                            matched_uuid = null,
                            score = 0,
                            is_enrolled = 0,
                            matched = null
                        };

                        responseByUuid.Add(uuid.ToString(), resp);

                        responses.Add(resp);
                    }

                    //get matches
                    if (await reader.NextResultAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            Guid uuid = reader.GetGuid(reader.GetOrdinal("uuid"));
                            Guid matched_uuid = reader.GetGuid(reader.GetOrdinal("matched_uuid"));
                            int score = reader.GetInt32(reader.GetOrdinal("score"));
                            int is_enrolled = reader.GetInt32(reader.GetOrdinal("is_enrolled"));
                            BiometricVerificationResponse response = responseByUuid[uuid.ToString()];
                            response.matched_uuid = matched_uuid.ToString();
                            response.score = score;
                            response.is_enrolled = is_enrolled;
                        }
                    }

                    //get household data
                    if (await reader.NextResultAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var uuid = reader.GetGuid(reader.GetOrdinal("rqst"));
                            var hohid = reader.GetString(reader.GetOrdinal("hohid"));

                            var hh = new FamilyProfile
                            {
                                householdUuid = reader.GetGuid(reader.GetOrdinal("householdUuid")).ToString(),
                                hohid = hohid,
                                type = reader.GetString(reader.GetOrdinal("type")),
                                members = new List<MemberInfo>()
                            };

                            familyByHouseholdUuid.Add(hohid, hh);

                            responseByUuid[uuid.ToString()].matched = hh;
                        }
                    }

                    //get individual data
                    if (await reader.NextResultAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var uuid = reader.GetGuid(reader.GetOrdinal("rqst"));
                            var hohid = reader.GetString(reader.GetOrdinal("householdId"));

                            var mem = new MemberInfo
                            {
                                uuid = reader.GetGuid(reader.GetOrdinal("uuid")).ToString(),
                                memno = reader.GetInt32(reader.GetOrdinal("memno")),
                                relationship = reader.GetString(reader.GetOrdinal("relationship")),
                                gender = reader.GetString(reader.GetOrdinal("gender")),
                                fullName = reader.GetString(reader.GetOrdinal("fullName")),
                                age = reader.GetInt32(reader.GetOrdinal("age")),
                                photoB64 = reader.GetString(reader.GetOrdinal("photoB64")),
                                has_biometric = reader.GetBoolean(reader.GetOrdinal("has_biometric"))
                            };

                            familyByHouseholdUuid[hohid].members.Add(mem);

                        }
                    }
                }

            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Error while Verifying Templates");
                throw;
            }

            _logger.LogInformation($"Verification responses: {responses.Count}");

            return responses;

            
        }

        public async Task<FlaggedData> DownloadFlaggedData(
    int tenantId,
    string partitionCode,
    string deviceId)
        {
            var result = new FlaggedData
            {
                households = new List<Household>(),
                individuals = new List<Individual>(),
                surveys = new List<SurveyAnswers>()
            };

            _logger.LogInformation($"Download Flagged Data, TenantId: {tenantId}, Partition code: {partitionCode}");

            try
            {
                await using var conn =
                new SqlConnection(_connectionString);

                await conn.OpenAsync();

                await using var cmd = new SqlCommand(
                    "dbo.sp_DownloadFlaggedData",
                    conn);

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@TenantId",
                    SqlDbType.Int).Value = tenantId;

                cmd.Parameters.Add("@PartitionCode",
                    SqlDbType.VarChar, 50).Value = partitionCode;

                cmd.Parameters.Add("@DeviceId",
                    SqlDbType.VarChar, 50).Value = deviceId;

                await using var reader =
                    await cmd.ExecuteReaderAsync();

                //
                // HOUSEHOLDS
                //
                while (await reader.ReadAsync())
                {
                    result.households.Add(new Household
                    {
                        uuid = reader.GetGuid(
                            reader.GetOrdinal("uuid")),

                        householdId = reader["householdId"]
                            ?.ToString(),

                        householdNo = reader.IsDBNull(
                            reader.GetOrdinal("householdNo"))
                            ? 0
                            : reader.GetInt32(
                                reader.GetOrdinal("householdNo")),

                        individualNo = reader.IsDBNull(
                            reader.GetOrdinal("individualNo"))
                            ? 0
                            : reader.GetInt32(
                                reader.GetOrdinal("individualNo")),

                        registrationToken = reader["registrationToken"]
                            ?.ToString(),

                        householdSize = reader.IsDBNull(
                            reader.GetOrdinal("householdSize"))
                            ? 0
                            : reader.GetInt32(
                                reader.GetOrdinal("householdSize")),

                        householdType = reader.IsDBNull(
                            reader.GetOrdinal("householdType"))
                            ? 0
                            : reader.GetInt32(
                                reader.GetOrdinal("householdType")),

                        locationJson = reader["locationJson"] == DBNull.Value
                            ? null
                            : JsonSerializer.Deserialize<object>(
                                reader["locationJson"].ToString()),

                        externalFamilyId = reader["externalFamilyId"]
                            ?.ToString(),

                        dataPoints = reader["dataPoints"] == DBNull.Value
                        ? new Dictionary<int, string>()
                        : JsonSerializer.Deserialize<Dictionary<int, string>>(
                            reader["dataPoints"].ToString()),

                        gpsLatLonAccuracy = reader["gpsLatLonAccuracy"]
                            ?.ToString(),

                        fromServer = true,

                        tenantId = reader.IsDBNull(
                            reader.GetOrdinal("TenantId"))
                            ? 0
                            : reader.GetInt32(
                                reader.GetOrdinal("TenantId")),

                        createdByUserId = reader["CreatedByUserId"]
                            ?.ToString(),

                        createdOnMs = new DateTimeOffset(
                            reader.GetDateTime(
                                reader.GetOrdinal("CreatedOn")))
                            .ToUnixTimeMilliseconds(),

                        updatedByUserId = reader["UpdatedByUserId"]
                            ?.ToString(),

                        updatedOnMs = reader.IsDBNull(
                            reader.GetOrdinal("UpdatedOn"))
                            ? 0
                            : new DateTimeOffset(
                                reader.GetDateTime(
                                    reader.GetOrdinal("UpdatedOn")))
                                .ToUnixTimeMilliseconds()
                    });
                }

                //
                // MOVE TO INDIVIDUALS RESULTSET
                //
                await reader.NextResultAsync();

                //
                // INDIVIDUALS
                //
                while (await reader.ReadAsync())
                {
                    result.individuals.Add(new Individual
                    {
                        Uuid = reader.GetGuid(
                            reader.GetOrdinal("uuid")),

                        HouseholdId = reader["householdId"]
                            ?.ToString(),

                        IndividualId = reader.IsDBNull(
                            reader.GetOrdinal("individualId"))
                            ? 0
                            : reader.GetInt32(
                                reader.GetOrdinal("individualId")),

                        FirstName = reader["firstName"]
                            ?.ToString(),

                        MiddleName = reader["middleName"]
                            ?.ToString(),

                        LastName = reader["lastName"]
                            ?.ToString(),

                        Dob = reader.IsDBNull(
                            reader.GetOrdinal("dob"))
                            ? null
                            : reader.GetDateTime(
                                reader.GetOrdinal("dob")),

                        AgeInYears = reader.IsDBNull(
                            reader.GetOrdinal("ageInYears"))
                            ? null
                            : reader.GetInt32(
                                reader.GetOrdinal("ageInYears")),

                        AgeInMonths = reader.IsDBNull(
                            reader.GetOrdinal("ageInMonths"))
                            ? null
                            : reader.GetInt32(
                                reader.GetOrdinal("ageInMonths")),

                        AgeInDays = reader.IsDBNull(
                            reader.GetOrdinal("ageInDays"))
                            ? null
                            : reader.GetInt32(
                                reader.GetOrdinal("ageInDays")),

                        Relationship = reader.IsDBNull(
                            reader.GetOrdinal("relationship"))
                            ? 0
                            : reader.GetInt32(
                                reader.GetOrdinal("relationship")),

                        Gender = reader.IsDBNull(
                            reader.GetOrdinal("gender"))
                            ? 0
                            : reader.GetInt32(
                                reader.GetOrdinal("gender")),

                        //dataPoints = new Dictionary<int, string>(),
                        dataPoints = reader["dataPoints"] == DBNull.Value
                        ? new Dictionary<int, string>()
                        : JsonSerializer.Deserialize<Dictionary<int, string>>(
                            reader["dataPoints"].ToString()),

                        ExternalIndividualId = reader["externalIndividualId"]
                            ?.ToString(),

                        PhotoBase64 = reader["photoBase64"]
                            ?.ToString(),

                        BiometricBase64 = reader["BiometricBytes"] == DBNull.Value
                        ? string.Empty
                        : Convert.ToBase64String(
                            (byte[])reader["BiometricBytes"]
                            ),

                        biometricNotCollected = new BiometricNotCollected(
                                reader["biometricNotCollected"] == DBNull.Value ? 1 : reader.GetInt32(reader.GetOrdinal("biometricNotCollected")),
                                reader["reasonIfOther"]?.ToString()
                            ),

                        tenantId = reader.IsDBNull(
                            reader.GetOrdinal("TenantId"))
                            ? 0
                            : reader.GetInt32(
                                reader.GetOrdinal("TenantId")),

                        createdByUserId = reader["CreatedByUserId"]
                            ?.ToString(),

                        createdOnMs = new DateTimeOffset(
                            reader.GetDateTime(
                                reader.GetOrdinal("CreatedOn")))
                            .ToUnixTimeMilliseconds(),

                        updatedByUserId = reader["UpdatedByUserId"]
                            ?.ToString(),

                        updatedOnMs = reader.IsDBNull(
                            reader.GetOrdinal("UpdatedOn"))
                            ? 0
                            : new DateTimeOffset(
                                reader.GetDateTime(
                                    reader.GetOrdinal("UpdatedOn")))
                                .ToUnixTimeMilliseconds()
                    });
                }

                //
                // MOVE TO SURVEYS RESULTSET
                //
                await reader.NextResultAsync();

                //
                // SURVEYS
                //
                while (await reader.ReadAsync())
                {
                    result.surveys.Add(new SurveyAnswers
                    {

                        survey_id = reader.GetInt32(
                                reader.GetOrdinal("SurveyId")),

                        household_id = reader["householdId"]
                            ?.ToString(),

                        individual_id = reader.IsDBNull(
                            reader.GetOrdinal("individualId"))
                            ? 0
                            : reader.GetInt32(
                                reader.GetOrdinal("individualId")),

                        answers = reader["Answers"]
                            ?.ToString(),

                      
                    });
                }
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Error while Downloading Flagged data");
                throw;
            }

            return result;
        }
    }
}
