using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using BRaVe_Management_Backend.Models.data_payload;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;
using static BRaVe_Management_Backend.Helpers.KeyVaultSecretNames;

namespace BRaVe_Management_Backend.Services
{
    public class SqlDataLoaderService : IDataLoaderService
    {
        private readonly IServiceBusSender _bus;
        private readonly ILogger<SqlDataLoaderService> _logger;
        private readonly string _connectionString;
        // inject DbContext, etc.

        public SqlDataLoaderService(ISecretProvider secretProvider, IServiceBusSender bus, ILogger<SqlDataLoaderService> logger)
        {
            _connectionString = secretProvider.GetSecretAsync(Sql.PrimaryConnection).Result;
            _bus = bus;
            _logger = logger;
        }

        public async Task LoadAsync(Guid JobId)
        {
            _logger.LogInformation("Starting data load for job {JobId}", JobId);

            /*const string sql = @"
                SELECT * FROM tbl_RegistrationActivityStaging WITH (READCOMMITTED)
                WHERE BatchId = @Id AND IsProcessed=0;";*/

            List<RegistrationActivityStaging> stagingRecords = new List<RegistrationActivityStaging>();

            // TODO: move from staging → final tables, validations, etc.
            try
            {
                using var cn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("sp_GetRegistrationActivity_ByBatchId", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.Add("@JobId", SqlDbType.UniqueIdentifier).Value = JobId;
                await cn.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {

                    var staging = new RegistrationActivityStaging
                    {

                        DeviceId = reader["DeviceId"].ToString(),
                        TenantId = (int)reader["TenantId"],
                        ActivityCode = reader["ActivityCode"].ToString(),
                        BatchId = JobId,
                        Type = (StagingType)(int)reader["Type"],
                        Payload = reader["Payload"].ToString()

                    };

                    stagingRecords.Add(staging);

                }

                if (stagingRecords.Count == 0)
                {
                    _logger.LogInformation("No staging data found for job {JobId}", JobId);
                    return;
                }

                await ProcessAll(stagingRecords);

                // MarkCompleted(JobId);

                _logger.LogInformation("Completed data load for job {JobId}", JobId);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Data load job {JobId} failed", JobId);
            }
        }

        private void MarkCompleted(Guid jobId)
        {
            _logger.LogInformation("Marking job {JobId} as completed", jobId);

            const string sql = @"
                    UPDATE tbl_RegistrationActivityStaging
                    SET isprocessed = 1,
                        processedon = GETUTCDATE()
                    WHERE BatchId = @JobId;
                ";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add("@JobId", SqlDbType.UniqueIdentifier).Value = jobId;

            conn.Open();
            var rowsAffected = cmd.ExecuteNonQuery();

            _logger.LogInformation(
                "Marked job {JobId} as completed. Rows affected: {Rows}",
                jobId,
                rowsAffected
            );
        }

        private async Task ProcessAll(List<RegistrationActivityStaging> stagingRecords)
        {
            bool hasBiometric = false;

            //Process household data first

            // 1) Sort the list by the enum value (priority)
            stagingRecords = stagingRecords
                .OrderBy(r => (int)r.Type)   // or just r.StagingType
                .ToList();

            // 2) Single loop, now in correct order:
            //    household -> individual -> consent -> survey -> distribution
            foreach (var record in stagingRecords)
            {
                switch (record.Type)
                {
                    case StagingType.household:
                        ProcessHousehold(record.BatchId, record.TenantId, record.ActivityCode, record.Payload);
                        break;

                    case StagingType.individual:

                        if (await ProcessIndividual(record.BatchId, record.TenantId, record.ActivityCode, record.Payload))
                        {
                            hasBiometric = true;
                        }

                        break;

                    case StagingType.consent:
                        ProcessConsent(record.BatchId, record.TenantId, record.ActivityCode, record.Payload);
                        break;

                    case StagingType.survey:
                        ProcessSurvey(record.BatchId, record.TenantId, record.ActivityCode, record.Payload);
                        break;

                    case StagingType.distribution:
                        ProcessDistribution(record.BatchId, record.TenantId, record.ActivityCode, record.Payload);
                        break;

                    case StagingType.verification:

                        if (await ProcessVerification(record.BatchId, record.TenantId, record.ActivityCode, record.Payload))
                        {
                            hasBiometric = true;
                        }
                        break;

                    case StagingType.del_audit:
                        ProcessDelAudit(record.BatchId, record.TenantId, record.ActivityCode, record.Payload);
                        break;
                }
            }

            MarkCompleted(stagingRecords.FirstOrDefault().BatchId);

            //send to Azure Service Bus for Biometric Matching processing
            if (hasBiometric && stagingRecords != null && stagingRecords.Count > 0)
            {
               await _bus.SendMessageAsync(stagingRecords.FirstOrDefault().BatchId.ToString());
            }
        }


        private async void ProcessDelAudit(Guid batchId, int tenantId, string activityCode, string payload)
        {
            _logger.LogInformation("job {JobId}, Processing Deletion Log record", batchId);

            DeletionLog del_log = JsonSerializer.Deserialize<DeletionLog>(payload);

            if (del_log == null)
                throw new ArgumentException("Invalid payload - cannot deserialize DeletionLog.", nameof(payload));

            using var cn = new SqlConnection(_connectionString);

            using var cmd = new SqlCommand("sp_InsertDeletionLog", cn)
            {
                CommandType = CommandType.StoredProcedure
            };

            // @UpdatedOnMs BIGINT = NULL
            cmd.Parameters.Add("@id", SqlDbType.BigInt).Value = del_log.id;

            cmd.Parameters.Add("@TenantId", SqlDbType.Int).Value = tenantId;

            cmd.Parameters.Add("@deletionType", SqlDbType.VarChar, 20).Value = del_log.deletionType;

            cmd.Parameters.Add("@householdId", SqlDbType.VarChar, 50).Value = del_log.householdId;

            cmd.Parameters.Add("@individualId", SqlDbType.Int).Value = DbValue(del_log.individualId);

            cmd.Parameters.Add("@deletedIndividualIds", SqlDbType.VarChar, -1).Value = DbValue(del_log.deletedIndividualIds);

            cmd.Parameters.Add("@justification", SqlDbType.NVarChar, 2000).Value = DbValue(del_log.justification);

            cmd.Parameters.Add("@deletedBy", SqlDbType.VarChar, 50).Value = DbValue(del_log.deletedBy);//DbValue();

            cmd.Parameters.Add("@deletedAtEpochMs", SqlDbType.BigInt).Value = del_log.deletedAt;

            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

        }


        private async Task<bool> ProcessVerification(Guid batchId, int tenantId, string activityCode, string payload)
        {
            _logger.LogInformation("job {JobId}, Processing verification record", batchId);

            BiometricVerificationRequest verification = JsonSerializer.Deserialize<BiometricVerificationRequest>(payload);

            if (verification == null)
                throw new ArgumentException("Invalid payload - cannot deserialize BiometricVerification.", nameof(payload));

            bool hasBiometric = false;

            byte[] biometricBytes = Convert.FromBase64String(verification.template);

            using var cn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_InsertVerification", cn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@batchId", SqlDbType.UniqueIdentifier).Value = batchId;
            cmd.Parameters.Add("@ActivityCode", SqlDbType.VarChar, 50).Value = activityCode;

            Guid uuid = new Guid(verification.uuid);

            cmd.Parameters.Add("@uuid", SqlDbType.UniqueIdentifier).Value = uuid;

            cmd.Parameters.Add("@gender", SqlDbType.Int).Value = DbValue(verification.gender);

            // @photoBase64 NVARCHAR(MAX)
            var biometric = cmd.Parameters.Add("@biometricBytes", SqlDbType.VarBinary, -1);
            biometric.Value = DbValue(biometricBytes);

            // @TenantId INT
            cmd.Parameters.Add("@TenantId", SqlDbType.Int)
                .Value = tenantId;

            // @CreatedByUserId VARCHAR(50)
            cmd.Parameters.Add("@CreatedByUserId", SqlDbType.VarChar, 50).Value = DbValue(verification.createdByUserId);

            // @CreatedOnMs BIGINT
            cmd.Parameters.Add("@CreatedOnMs", SqlDbType.BigInt).Value = DbValue(verification.createdOnMs);

            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            //_logger.LogInformation("job {JobId}, verification record processed successfully.", batchId);

            return true;
        }

        private async Task ProcessDistribution(Guid batchId, int tenantId, string activityCode, string payload)
        {

            _logger.LogInformation("job {JobId}, Processing distribution record", batchId);

            StgDistributionAssistance distributionAssistance = JsonSerializer.Deserialize<StgDistributionAssistance>(payload);

            if (distributionAssistance == null)
                throw new ArgumentException("Invalid payload - cannot deserialize DistributionAssistance.", nameof(payload));

            if (string.IsNullOrWhiteSpace(distributionAssistance.assistances))
                return; // nothing to process

            DistributionSelectionDto distributionSelectionDto =
                JsonSerializer.Deserialize<DistributionSelectionDto>(distributionAssistance.assistances);

            Guid individualGuid = Guid.Empty;

            foreach (var ind in distributionSelectionDto.Selections)
            {
                if (ind.Selected)
                {
                    individualGuid = new Guid(ind.Uuid);
                    break;
                }
            }


            //Starts here
            using var cn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_UpsertAssistance", cn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@ActivityCode", SqlDbType.VarChar, 50).Value = activityCode;
            cmd.Parameters.Add("@distributionId", SqlDbType.Int).Value = distributionAssistance.distributionId;
            // @TenantId INT
            cmd.Parameters.Add("@TenantId", SqlDbType.Int).Value = tenantId;
            cmd.Parameters.Add("@householdId", SqlDbType.UniqueIdentifier).Value = new Guid(distributionSelectionDto.FamilyUuid);
            cmd.Parameters.Add("@individualId", SqlDbType.UniqueIdentifier).Value = individualGuid;
            cmd.Parameters.Add("@BiometricConfirmation", SqlDbType.Bit).Value = distributionSelectionDto.BiometricConfirmation;
            cmd.Parameters.Add("@PhotoConfirmation", SqlDbType.Bit).Value = distributionSelectionDto.PhotoConfirmation;
            var pPhoto = cmd.Parameters.Add("@PhotoBase64", SqlDbType.NVarChar, -1);
            pPhoto.Value = DbValue(distributionSelectionDto.PhotoBase64);

            cmd.Parameters.Add("@data", SqlDbType.VarChar, -1).Value = JsonSerializer.Serialize(distributionSelectionDto.ItemSelections);


            cmd.Parameters.Add("@comment", SqlDbType.VarChar, -1).Value = DbValue(distributionSelectionDto.Comment);

            // @CreatedByUserId VARCHAR(50)
            cmd.Parameters.Add("@CreatedByUserId", SqlDbType.VarChar, 50).Value = distributionSelectionDto.CreatedBy;

            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

        }

        private async Task ProcessSurvey(Guid batchId, int tenantId, string activityCode, string payload)
        {
            _logger.LogInformation("job {JobId}, Processing survey record", batchId);

            SurveyAnswers survey_answers = JsonSerializer.Deserialize<SurveyAnswers>(payload);

            if (survey_answers == null)
                throw new ArgumentException("Invalid payload - cannot deserialize SurveyAnswers.", nameof(payload));

            if (string.IsNullOrWhiteSpace(survey_answers.answers))
                return; // nothing to process

            // data = {"1":"John Doe","2":"2.0","3":"2"}
            Dictionary<string, string> answersDict =
                JsonSerializer.Deserialize<Dictionary<string, string>>(survey_answers.answers);

            using var cn = new SqlConnection(_connectionString);
            await cn.OpenAsync();

            foreach (var kv in answersDict)
            {
                if (!int.TryParse(kv.Key, out int questionId))
                    continue; // skip invalid question ids if any

                using var cmd = new SqlCommand("sp_UpsertSurveyQuestions", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.Add("@batchId", SqlDbType.UniqueIdentifier).Value = batchId;

                // @SurveyCode NVARCHAR(100)
                cmd.Parameters.Add("@SurveyId", SqlDbType.Int)
                    .Value = DbValue(survey_answers.survey_id);

                // @HouseholdId VARCHAR(50)
                cmd.Parameters.Add("@HouseholdId", SqlDbType.VarChar, 50)
                    .Value = DbValue(survey_answers.household_id);

                // @IndividualId INT
                cmd.Parameters.Add("@IndividualId", SqlDbType.Int)
                    .Value = DbValue(survey_answers.individual_id > 0 ? survey_answers.individual_id : (int?)null);

                // @QuestionId INT
                cmd.Parameters.Add("@QuestionId", SqlDbType.Int)
                    .Value = questionId;

                // @QuestionAnswer NVARCHAR(MAX)
                cmd.Parameters.Add("@QuestionAnswer", SqlDbType.NVarChar, -1)
                    .Value = DbValue(kv.Value);

                // @TenantId INT
                cmd.Parameters.Add("@TenantId", SqlDbType.Int)
                    .Value = tenantId;

                // @UpdatedByUserId VARCHAR(50)
                cmd.Parameters.Add("@UpdatedByUserId", SqlDbType.VarChar, 50)
                    .Value = DBNull.Value; // or pass from payload if available

                // @UpdatedOnMs BIGINT
                cmd.Parameters.Add("@UpdatedOnMs", SqlDbType.BigInt)
                    .Value = DBNull.Value;

                await cmd.ExecuteNonQueryAsync();
            }
        }

        private async Task ProcessConsent(Guid batchId, int tenantId, string activityCode, string payload)
        {
            _logger.LogInformation("job {JobId}, Processing consent record", batchId);
            ConsentFeedback feedback = JsonSerializer.Deserialize<ConsentFeedback>(payload);

            using var cn = new SqlConnection(_connectionString);

            using var cmd = new SqlCommand("sp_InsertConsentFeedback", cn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@batchId", SqlDbType.UniqueIdentifier).Value = batchId;
            cmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = feedback.ConsentId;
            cmd.Parameters.Add("@ActivityCode", SqlDbType.VarChar, 50).Value = activityCode;
            cmd.Parameters.Add("@TenantId", SqlDbType.Int).Value = tenantId;
            cmd.Parameters.Add("@Payload", SqlDbType.NVarChar, -1).Value = feedback.Data;
            cmd.Parameters.Add("@Type", SqlDbType.Int).Value = DBNull.Value;
            cmd.Parameters.Add("@ConsentNotProvided", SqlDbType.Bit).Value = true;
            cmd.Parameters.Add("@InsertedBy", SqlDbType.VarChar, 50).Value = feedback.InsertedBy;
            cmd.Parameters.Add("@LongValue", SqlDbType.BigInt).Value = feedback.InsertedOn;
            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

        }

        private async Task<bool> ProcessIndividual(Guid batchId, int tenantId, string activityCode, string payload)
        {
            _logger.LogInformation("job {JobId}, Processing individual record", batchId);

            Individual ind = JsonSerializer.Deserialize<Individual>(payload);
            if (ind == null)
                throw new ArgumentException("Invalid payload - cannot deserialize Individual.", nameof(payload));

            bool hasBiometric = false;

            byte[] biometricBytes = Array.Empty<byte>();

            string biometricBase64 = ind.BiometricBase64;

            if (!string.IsNullOrEmpty(biometricBase64))
            {
                biometricBytes = Convert.FromBase64String(biometricBase64);
                hasBiometric = true;
            }

            using var cn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_UpsertIndividualV2", cn)
            {
                CommandType = CommandType.StoredProcedure
            };

            // @uuid UNIQUEIDENTIFIER
            if (ind.Uuid == Guid.Empty)
                throw new ArgumentException("uuid is required and cannot be empty.", nameof(ind.Uuid));

            cmd.Parameters.Add("@batchId", SqlDbType.UniqueIdentifier).Value = batchId;

            cmd.Parameters.Add("@uuid", SqlDbType.UniqueIdentifier).Value = ind.Uuid;

            // @householdId VARCHAR(50)
            cmd.Parameters.Add("@householdId", SqlDbType.VarChar, 50).Value = ind.HouseholdId;

            // @individualId INT
            cmd.Parameters.Add("@individualId", SqlDbType.Int).Value = ind.IndividualId;

            // prepare dataPoints JSON
            string dataPointsJson = ind.dataPoints != null ? JsonSerializer.Serialize(ind.dataPoints) : null;

            // parse Dob (object -> DateTime?)
            DateTime? dob = null;
            if (ind.Dob != null)
            {
                if (ind.Dob is DateTime dt) dob = dt.Date;
                else if (DateTime.TryParse(ind.Dob.ToString(), out var parsed)) dob = parsed.Date;
            }

            // parse ages (object -> int?)
            int? ageYears = null, ageMonths = null, ageDays = null;
            if (ind.AgeInYears != null && int.TryParse(ind.AgeInYears.ToString(), out var ay)) ageYears = ay;
            if (ind.AgeInMonths != null && int.TryParse(ind.AgeInMonths.ToString(), out var am)) ageMonths = am;
            if (ind.AgeInDays != null && int.TryParse(ind.AgeInDays.ToString(), out var ad)) ageDays = ad;

            // @firstName NVARCHAR(255)
            cmd.Parameters.Add("@firstName", SqlDbType.NVarChar, 255).Value = DbValue(ind.FirstName);

            // @middleName NVARCHAR(255)
            cmd.Parameters.Add("@middleName", SqlDbType.NVarChar, 255).Value = DbValue(ind.MiddleName);

            // @lastName NVARCHAR(255)
            cmd.Parameters.Add("@lastName", SqlDbType.NVarChar, 255).Value = DbValue(ind.LastName);

            // @dob DATE
            var pDob = cmd.Parameters.Add("@dob", SqlDbType.Date);
            pDob.Value = DbValue(dob);

            // @ageInYears INT
            cmd.Parameters.Add("@ageInYears", SqlDbType.Int).Value = DbValue(ageYears);

            // @ageInMonths INT
            cmd.Parameters.Add("@ageInMonths", SqlDbType.Int).Value = DbValue(ageMonths);

            // @ageInDays INT
            cmd.Parameters.Add("@ageInDays", SqlDbType.Int).Value = DbValue(ageDays);

            // @relationship INT
            cmd.Parameters.Add("@relationship", SqlDbType.Int).Value = DbValue(ind.Relationship);

            // @gender INT
            cmd.Parameters.Add("@gender", SqlDbType.Int).Value = DbValue(ind.Gender);

            // @dataPoints NVARCHAR(MAX)
            var pDataPoints = cmd.Parameters.Add("@dataPoints", SqlDbType.NVarChar, -1);
            pDataPoints.Value = DbValue(dataPointsJson);

            // @externalIndividualId NVARCHAR(100)
            cmd.Parameters.Add("@externalIndividualId", SqlDbType.NVarChar, 100).Value = DbValue(ind.ExternalIndividualId);

            // @photoBase64 NVARCHAR(MAX)
            var pPhoto = cmd.Parameters.Add("@photoBase64", SqlDbType.NVarChar, -1);
            pPhoto.Value = DbValue(ind.PhotoBase64);

            // @photoBase64 NVARCHAR(MAX)
            var biometric = cmd.Parameters.Add("@biometricBytes", SqlDbType.VarBinary, -1);
            biometric.Value = DbValue(biometricBytes);


            //for old version to be compatible
            if (ind.biometricNotCollected == null)
            { 
                ind.biometricNotCollected = new BiometricNotCollected();
                ind.biometricNotCollected.selectedReason = 1;
            }

            //Biometric was not collected. Why?
            cmd.Parameters.Add("@biometricNotCollected", SqlDbType.Int).Value = DbValue(ind.biometricNotCollected.selectedReason);
            var reasonIfOth = cmd.Parameters.Add("@reasonIfOther", SqlDbType.NVarChar, -1);
            reasonIfOth.Value = DbValue(ind.biometricNotCollected.reasonIfOther);

            // @TenantId INT (prefer payload, fallback to method param)
            var tenantValue = ind.tenantId > 0 ? ind.tenantId : tenantId;
            cmd.Parameters.Add("@TenantId", SqlDbType.Int).Value = tenantValue;

            // @CreatedByUserId VARCHAR(50)
            cmd.Parameters.Add("@CreatedByUserId", SqlDbType.VarChar, 50).Value = DbValue(ind.createdByUserId);

            // @CreatedOnMs BIGINT
            cmd.Parameters.Add("@CreatedOnMs", SqlDbType.BigInt).Value = DbValue(ind.createdOnMs);

            // @UpdatedByUserId VARCHAR(50)
            cmd.Parameters.Add("@UpdatedByUserId", SqlDbType.VarChar, 50).Value = DbValue(ind.updatedByUserId);

            // @UpdatedOnMs BIGINT
            cmd.Parameters.Add("@UpdatedOnMs", SqlDbType.BigInt).Value = DbValue(ind.updatedOnMs);

            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            return hasBiometric;
        }

        private async Task ProcessHousehold(Guid batchId, int tenantId, string activityCode, string payload)
        {
            _logger.LogInformation("job {JobId}, Processing household record", batchId);

            Household hh = JsonSerializer.Deserialize<Household>(payload);

            using var cn = new SqlConnection(_connectionString);

            using var cmd = new SqlCommand("sp_UpsertHousehold", cn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@batchId", SqlDbType.UniqueIdentifier).Value = batchId;

            // @uuid UNIQUEIDENTIFIER
            cmd.Parameters.Add("@uuid", SqlDbType.UniqueIdentifier).Value = hh.uuid;

            // @householdId VARCHAR(50) = NULL
            cmd.Parameters.Add("@householdId", SqlDbType.VarChar, 50).Value = hh.householdId;

            // @householdNo INT
            cmd.Parameters.Add("@householdNo", SqlDbType.Int).Value = hh.householdNo;

            // @individualNo INT
            cmd.Parameters.Add("@individualNo", SqlDbType.Int).Value = hh.individualNo;

            // @activityCode VARCHAR(10)
            cmd.Parameters.Add("@activityCode", SqlDbType.VarChar, 10).Value = activityCode;

            // @registrationToken NVARCHAR(255) = NULL
            cmd.Parameters.Add("@registrationToken", SqlDbType.NVarChar, 255).Value = hh.registrationToken;

            // @householdSize INT = NULL
            cmd.Parameters.Add("@householdSize", SqlDbType.Int).Value = hh.householdSize;

            // @householdType INT = NULL
            cmd.Parameters.Add("@householdType", SqlDbType.Int).Value = hh.householdType;

            // @TenantId INT (prefer payload, fallback to method param)

            cmd.Parameters.Add("@TenantId", SqlDbType.Int).Value = tenantId;

            // @locationJson NVARCHAR(250) = NULL
            cmd.Parameters.Add("@locationJson", SqlDbType.NVarChar, 250).Value = DbValue(hh.locationJson?.ToString());

            // @collectionSource VARCHAR(1)
            cmd.Parameters.Add("@collectionSource", SqlDbType.VarChar, 1).Value = hh.collectionSource;

            // @externalFamilyId NVARCHAR(100) = NULL
            cmd.Parameters.Add("@externalFamilyId", SqlDbType.NVarChar, 100).Value = DbValue(hh.externalFamilyId);

            string datapointsJson = null;

            if (hh.dataPoints != null)
                datapointsJson = JsonSerializer.Serialize(hh.dataPoints);

            // @dataPoints NVARCHAR(MAX) = NULL  (size = -1 for MAX)
            var pDataPoints = cmd.Parameters.Add("@dataPoints", SqlDbType.NVarChar, -1);
            pDataPoints.Value = DbValue(datapointsJson);

            // @gpsLatLonAccuracy NVARCHAR(150) = NULL
            cmd.Parameters.Add("@gpsLatLonAccuracy", SqlDbType.NVarChar, 150).Value = DbValue(hh.gpsLatLonAccuracy?.ToString());

            // @CreatedByUserId VARCHAR(50)
            cmd.Parameters.Add("@CreatedByUserId", SqlDbType.VarChar, 50).Value = hh.createdByUserId;

            // @CreatedOnMs BIGINT = NULL
            cmd.Parameters.Add("@CreatedOnMs", SqlDbType.BigInt).Value = DbValue(hh.createdOnMs);

            // @UpdatedByUserId VARCHAR(50) = NULL
            cmd.Parameters.Add("@UpdatedByUserId", SqlDbType.VarChar, 50).Value = DbValue(hh.updatedByUserId);

            // @UpdatedOnMs BIGINT = NULL
            cmd.Parameters.Add("@UpdatedOnMs", SqlDbType.BigInt).Value = DbValue(hh.updatedOnMs);

            try
            {
                await cn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
            catch(Exception ex) { 
            
            }
           

        }

        static object DbValue<T>(T value) => value is null ? DBNull.Value : (object)value;


    }
}
