using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Data;

namespace BRaVe_Management_Backend.Services
{
    public class SqlRiskBenefitAssessmentService : IRiskBenefitAssessment
    {
        private readonly string connectionString;
        private readonly ILogger<SqlRiskBenefitAssessmentService> _logger;

        public SqlRiskBenefitAssessmentService(ISecretProvider secretProvider, ILogger<SqlRiskBenefitAssessmentService> logger)
        {
            _logger = logger;
            connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection).Result;
        }

        public async Task<RiskBenefitAssessment> CreateRiskBenefitAssessment(RiskBenefitAssessmentDto data, string UserId)
        {
            _logger.LogInformation("Creating RiskBenefitAssessment for tenant {TenantId} by user {UserId}", data.TenantId, UserId);

            using var conn = new SqlConnection(connectionString);
            var cmd = new SqlCommand(@"sp_CreateRiskBenefitAssessment @TenantId, @ProgramId, @PmName, @PmContacts, @RequireApprovals, @CreatedByUserId", conn);

            cmd.Parameters.AddWithValue("@TenantId", data.TenantId);
            cmd.Parameters.AddWithValue("@ProgramId", data.ProgramId);
            cmd.Parameters.AddWithValue("@PmName", data.ProgamManagerName);
            cmd.Parameters.AddWithValue("@PmContacts", data.ProgamManagerContacts);
            cmd.Parameters.AddWithValue("@RequireApprovals", data.RequireApprovals);
            cmd.Parameters.AddWithValue("@CreatedByUserId", UserId);

            try
            {
                await conn.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var result = new RiskBenefitAssessment
                    {
                        AssessmentId = reader.GetInt32(reader.GetOrdinal("AssessmentId")),
                        RequireApprovals = reader.GetBoolean(reader.GetOrdinal("RequireApprovals")),
                        StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
                        CreatedOn = reader.GetDateTime(reader.GetOrdinal("CreatedOn"))
                    };

                    _logger.LogInformation("Created RiskBenefitAssessment {AssessmentId} for tenant {TenantId}", result.AssessmentId, data.TenantId);
                    return result;
                }

                _logger.LogWarning("No RiskBenefitAssessment record returned for tenant {TenantId}", data.TenantId);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while creating RiskBenefitAssessment for tenant {TenantId} by user {UserId}", data.TenantId, UserId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating RiskBenefitAssessment for tenant {TenantId}", data.TenantId);
                throw;
            }

            return null;
        }

        public async Task<List<RiskBenefitAssessment>> GetAllAssessments(int TenantId, string Language)
        {
            _logger.LogInformation("Fetching all RiskBenefitAssessments for tenant {TenantId} with language {Language}", TenantId, Language);

            var assessments = new List<RiskBenefitAssessment>();
            using var conn = new SqlConnection(connectionString);
            var cmd = new SqlCommand(@"sp_GetAllAssessments @TenantId, @Lang", conn);
            cmd.Parameters.AddWithValue("@TenantId", TenantId);
            cmd.Parameters.AddWithValue("@Lang", Language);

            try
            {
                await conn.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    assessments.Add(new RiskBenefitAssessment
                    {
                        AssessmentId = reader.GetInt32(reader.GetOrdinal("AssessmentId")),
                        PmName = reader.IsDBNull(reader.GetOrdinal("PmName")) ? null : reader.GetString(reader.GetOrdinal("PmName")),
                        StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
                        StatusText = reader.GetString(reader.GetOrdinal("StatusText")),
                        CreatedByUserId = reader.GetString(reader.GetOrdinal("CreatedByUserId")),
                        CreatedOn = reader.GetDateTime(reader.GetOrdinal("CreatedOn"))
                    });
                }

                _logger.LogInformation("Fetched {Count} assessments for tenant {TenantId}", assessments.Count, TenantId);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while fetching all assessments for tenant {TenantId}", TenantId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching all assessments for tenant {TenantId}", TenantId);
                throw;
            }

            return assessments;
        }

        public async Task<RiskBenefitAssessmentMetadataDto> GetAssessmentById(int AssessmentId, int TenantId, string Language)
        {
            _logger.LogInformation("Fetching RiskBenefitAssessment metadata for AssessmentId {AssessmentId}, TenantId {TenantId}, Language {Language}", AssessmentId, TenantId, Language);

            RiskBenefitAssessmentMetadataDto riskBenefitAssessment = new();
            using var conn = new SqlConnection(connectionString);
            var cmd = new SqlCommand(@"sp_GetRiskBenefitAssessmentData @AssessmentId, @Lang", conn);
            cmd.Parameters.AddWithValue("@AssessmentId", AssessmentId);
            cmd.Parameters.AddWithValue("@Lang", Language);

            try
            {
                await conn.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();

                //Get tbl_RiskBenefitAssessments
                if (await reader.ReadAsync())
                {
                    riskBenefitAssessment.AssessmentId = AssessmentId;
                    riskBenefitAssessment.TenantId = TenantId;
                    riskBenefitAssessment.StatusId = reader.GetInt32(reader.GetOrdinal("StatusId"));
                    riskBenefitAssessment.RequireApprovals = reader.GetBoolean(reader.GetOrdinal("RequireApprovals"));
                    riskBenefitAssessment.CreatedByUserId = reader.GetString(reader.GetOrdinal("CreatedByUserId"));
                    riskBenefitAssessment.CreatedOn = reader.GetDateTime(reader.GetOrdinal("CreatedOn"));
                }

                //Get tbl_DataProcessing
                reader.NextResult();
                if (await reader.ReadAsync())
                {
                    riskBenefitAssessment.DataProcessing = new DataProcessing
                    {
                        AssessmentId = AssessmentId,
                        ProgamManagerName = reader.GetString(reader.GetOrdinal("ProgamManagerName")),
                        ProgamManagerContacts = reader.GetString(reader.GetOrdinal("ProgamManagerContacts")),
                        DataManagerName = reader.IsDBNull(reader.GetOrdinal("DataManagerName")) ? null : reader.GetString(reader.GetOrdinal("DataManagerName")),
                        DataManagerContacts = reader.IsDBNull(reader.GetOrdinal("DataManagerContacts")) ? null : reader.GetString(reader.GetOrdinal("DataManagerContacts")),
                        PrimaryPurposeId = reader.IsDBNull(reader.GetOrdinal("PrimaryPurposeId")) ? null : reader.GetInt32(reader.GetOrdinal("PrimaryPurposeId")),
                        PrimaryPurposeOther = reader.IsDBNull(reader.GetOrdinal("PrimaryPurposeOther")) ? null : reader.GetString(reader.GetOrdinal("PrimaryPurposeOther")),
                        SecondaryPurposeId = reader.IsDBNull(reader.GetOrdinal("SecondaryPurposeId")) ? null : reader.GetInt32(reader.GetOrdinal("SecondaryPurposeId")),
                        SecondaryPurposeOther = reader.IsDBNull(reader.GetOrdinal("SecondaryPurposeOther")) ? null : reader.GetString(reader.GetOrdinal("SecondaryPurposeOther"))

                    };

                }

                List<DataProcessingCollaborator> Collaborators = new();
                //Get List of tbl_DataProcessingCollaborators
                reader.NextResult();
                while (await reader.ReadAsync())
                {
                    Collaborators.Add(new DataProcessingCollaborator
                    {
                        AssessmentId = AssessmentId,
                        PartnerId = reader.GetInt32(reader.GetOrdinal("PartnerId")),
                        PartnerOrganizationName = reader.GetString(reader.GetOrdinal("PartnerOrganizationName")),
                        PartnerOrganizationContacts = reader.GetString(reader.GetOrdinal("PartnerOrganizationContacts")),
                        Description = reader.GetString(reader.GetOrdinal("Description")),
                        FilePath = reader.IsDBNull(reader.GetOrdinal("FilePath")) ? null : reader.GetString(reader.GetOrdinal("FilePath"))
                    });
                }

                riskBenefitAssessment.Collaborators = Collaborators;

                List<DataProcessingDataSubject> subjects = new();
                //Get List of DataProcessingDataSubject
                reader.NextResult();
                while (await reader.ReadAsync())
                {
                    subjects.Add(new DataProcessingDataSubject
                    {
                        AssessmentId = AssessmentId,
                        SubjectTypeId = reader.GetInt32(reader.GetOrdinal("SubjectTypeId")),
                        IsVulnerableGroup = reader.GetBoolean(reader.GetOrdinal("IsVunerableGroup")),
                    });
                }

                riskBenefitAssessment.Subjects = subjects;

                List<DataProcessingPersonalData> personalData = new();
                //Get List of DataProcessingPersonalData
                reader.NextResult();
                while (await reader.ReadAsync())
                {
                    personalData.Add(new DataProcessingPersonalData
                    {
                        AssessmentId = AssessmentId,
                        PersonalDataCategoryId = reader.GetInt32(reader.GetOrdinal("PersonalDataCategoryId")),
                        IsSpecialCategory = reader.GetBoolean(reader.GetOrdinal("IsSpecialCategory")),
                        PurposeOfDataCollection = reader.IsDBNull(reader.GetOrdinal("PurposeOfDataCollection")) ? null : reader.GetString(reader.GetOrdinal("PurposeOfDataCollection"))
                    });
                }

                riskBenefitAssessment.PersonalData = personalData;

                //Get DataProcessingLawfulBasis
                reader.NextResult();
                if (await reader.ReadAsync())
                {
                    riskBenefitAssessment.LawfulBasis = new DataProcessingLawfulBasis
                    {
                        AssessmentId = AssessmentId,
                        ProcessingActivityLawfulBasisId = reader.GetInt32(reader.GetOrdinal("ProcessingActivityLawfulBasisId")),
                        LawfulBasisExplanation = reader.IsDBNull(reader.GetOrdinal("LawfulBasisExplanation")) ? null : reader.GetString(reader.GetOrdinal("LawfulBasisExplanation")),
                        LawfulBasisDocPath = reader.IsDBNull(reader.GetOrdinal("LawfulBasisDocPath")) ? null : reader.GetString(reader.GetOrdinal("LawfulBasisDocPath")),
                        InitialDataCollectionExplanation = reader.GetString(reader.GetOrdinal("InitialDataCollectionExplanation")),
                        InitialDataCollectionDocPath = reader.IsDBNull(reader.GetOrdinal("InitialDataCollectionDocPath")) ? null : reader.GetString(reader.GetOrdinal("InitialDataCollectionDocPath")),
                        OngoingDataManagementExplanation = reader.GetString(reader.GetOrdinal("OngoingDataManagementExplanation")),
                        OngoingDataManagementDocPath = reader.IsDBNull(reader.GetOrdinal("OngoingDataManagementDocPath")) ? null : reader.GetString(reader.GetOrdinal("OngoingDataManagementDocPath")),
                        DataSharingExplanation = reader.GetString(reader.GetOrdinal("DataSharingExplanation")),
                        DataSharingDocPath = reader.IsDBNull(reader.GetOrdinal("DataSharingDocPath")) ? null : reader.GetString(reader.GetOrdinal("DataSharingDocPath"))
                    };

                }

                List<DataProcessingDataDisclosureRecipient> dataDisclosureRecipients = new();
                //Get List<DataProcessingDataDisclosureRecipient>
                reader.NextResult();
                while (await reader.ReadAsync())
                {
                    dataDisclosureRecipients.Add(new DataProcessingDataDisclosureRecipient
                    {
                        AssessmentId = AssessmentId,
                        RecipientId = reader.GetInt32(reader.GetOrdinal("RecipientId")),
                        RecipientOther = reader.IsDBNull(reader.GetOrdinal("RecipientOther")) ? null : reader.GetString(reader.GetOrdinal("RecipientOther")),
                        NameOfRecipientDepartment = reader.GetString(reader.GetOrdinal("NameOfRecipientDepartment")),
                        PurposeOfDataDisclosure = reader.IsDBNull(reader.GetOrdinal("PurposeOfDataDisclosure")) ? null : reader.GetString(reader.GetOrdinal("PurposeOfDataDisclosure")),
                        IsDataAggregatedBeforeSharing = reader.GetBoolean(reader.GetOrdinal("IsDataAggregatedBeforeSharing")),
                        IsDataAnonymizedBeforeSharing = reader.GetBoolean(reader.GetOrdinal("IsDataAnonymizedBeforeSharing")),
                        PurposeOfDataAccess = reader.IsDBNull(reader.GetOrdinal("PurposeOfDataAccess")) ? null : reader.GetString(reader.GetOrdinal("PurposeOfDataAccess")),
                    });

                }

                riskBenefitAssessment.DisclosureRecipients = dataDisclosureRecipients;


                List<DataProcessingDataSharingRecipient> dataSharingRecipients = new();
                //Get List<DataProcessingDataSharingRecipient>
                reader.NextResult();
                while (await reader.ReadAsync())
                {
                    dataSharingRecipients.Add(new DataProcessingDataSharingRecipient
                    {
                        AssessmentId = AssessmentId,
                        RecipientId = reader.GetInt32(reader.GetOrdinal("RecipientId")),
                        RecipientOther = reader.IsDBNull(reader.GetOrdinal("RecipientOther")) ? null : reader.GetString(reader.GetOrdinal("RecipientOther")),
                        TypeOfAgreementId = reader.GetInt32(reader.GetOrdinal("TypeOfAgreementId")),
                        TypeOfAgreementOther = reader.IsDBNull(reader.GetOrdinal("TypeOfAgreementOther")) ? null : reader.GetString(reader.GetOrdinal("TypeOfAgreementOther")),
                        StatusOfAgreementId = reader.GetInt32(reader.GetOrdinal("StatusOfAgreementId")),
                        PurposeOfDataSharing = reader.IsDBNull(reader.GetOrdinal("PurposeOfDataSharing")) ? null : reader.GetString(reader.GetOrdinal("PurposeOfDataSharing"))

                    });

                }

                riskBenefitAssessment.SharingRecipients = dataSharingRecipients;

                //Get DataProcessingRetention
                reader.NextResult();
                if (await reader.ReadAsync())
                {
                    riskBenefitAssessment.Retention = new DataProcessingRetention
                    {
                        AssessmentId = AssessmentId,
                        BeneficiaryDataYearsAfterProjectClosure = reader.IsDBNull(reader.GetOrdinal("BeneficiaryDataYearsAfterProjectClosure")) ? null : reader.GetInt32(reader.GetOrdinal("BeneficiaryDataYearsAfterProjectClosure")),
                        BeneficiaryDataMonthsAfterProjectClosure = reader.IsDBNull(reader.GetOrdinal("BeneficiaryDataMonthsAfterProjectClosure")) ? null : reader.GetInt32(reader.GetOrdinal("BeneficiaryDataMonthsAfterProjectClosure")),
                        BeneficiaryDataRetentionJustification = reader.GetString(reader.GetOrdinal("BeneficiaryDataRetentionJustification")),

                        FinancialDataYearsAfterProjectClosure = reader.IsDBNull(reader.GetOrdinal("FinancialDataYearsAfterProjectClosure")) ? null : reader.GetInt32(reader.GetOrdinal("FinancialDataYearsAfterProjectClosure")),
                        FinancialDataMonthsAfterProjectClosure = reader.IsDBNull(reader.GetOrdinal("FinancialDataMonthsAfterProjectClosure")) ? null : reader.GetInt32(reader.GetOrdinal("FinancialDataMonthsAfterProjectClosure")),
                        FinancialDataRetentionJustification = reader.GetString(reader.GetOrdinal("FinancialDataRetentionJustification")),

                        ConsentFormsYearsAfterProjectClosure = reader.IsDBNull(reader.GetOrdinal("ConsentFormsYearsAfterProjectClosure")) ? null : reader.GetInt32(reader.GetOrdinal("ConsentFormsYearsAfterProjectClosure")),
                        ConsentFormsMonthsAfterProjectClosure = reader.IsDBNull(reader.GetOrdinal("ConsentFormsMonthsAfterProjectClosure")) ? null : reader.GetInt32(reader.GetOrdinal("ConsentFormsMonthsAfterProjectClosure")),
                        ConsentFormsRetentionJustification = reader.GetString(reader.GetOrdinal("ConsentFormsRetentionJustification")),

                        MethodOfArchivingAfterProjectClosure = reader.IsDBNull(reader.GetOrdinal("MethodOfArchivingAfterProjectClosure")) ? null : reader.GetInt32(reader.GetOrdinal("MethodOfArchivingAfterProjectClosure")),
                        MethodOfArchivingAfterProjectClosureOther = reader.GetString(reader.GetOrdinal("MethodOfArchivingAfterProjectClosureOther")),
                        MethodOfDisposalAfterRetentionPeriod = reader.IsDBNull(reader.GetOrdinal("MethodOfDisposalAfterRetentionPeriod")) ? null : reader.GetInt32(reader.GetOrdinal("MethodOfDisposalAfterRetentionPeriod")),
                        MethodOfDisposalAfterRetentionPeriodOther = reader.GetString(reader.GetOrdinal("MethodOfDisposalAfterRetentionPeriodOther")),


                    };
                }


                List<DataProcessingSecurityMeasure> securityMeasures = new();
                //Get List<DataProcessingSecurityMeasure>
                reader.NextResult();
                while (await reader.ReadAsync())
                {
                    securityMeasures.Add(new DataProcessingSecurityMeasure
                    {
                        AssessmentId = AssessmentId,
                        SecurityMeasureId = reader.GetInt32(reader.GetOrdinal("SecurityMeasureId")),
                        TechnicalMeasures = reader.GetString(reader.GetOrdinal("TechnicalMeasures")),
                        OrganizationalMeasures = reader.GetString(reader.GetOrdinal("OrganizationalMeasures")),
                    });
                }

                riskBenefitAssessment.SecurityMeasures = securityMeasures;

                //Get DataProcessingSourceOfData
                reader.NextResult();
                if (await reader.ReadAsync())
                {
                    riskBenefitAssessment.SourceOfData = new DataProcessingSourceOfData
                    {
                        AssessmentId = AssessmentId,
                        PrimarySourceId = reader.GetInt32(reader.GetOrdinal("PrimarySourceId")),
                        PrimarySourceOther = reader.IsDBNull(reader.GetOrdinal("PrimarySourceOther")) ? null : reader.GetString(reader.GetOrdinal("PrimarySourceOther")),
                        SecondarySourceId = reader.IsDBNull(reader.GetOrdinal("SecondarySourceId")) ? null : reader.GetInt32(reader.GetOrdinal("SecondarySourceId")),
                        SecondarySourceOther = reader.IsDBNull(reader.GetOrdinal("SecondarySourceOther")) ? null : reader.GetString(reader.GetOrdinal("SecondarySourceOther")),
                        AttachmentUrl = reader.IsDBNull(reader.GetOrdinal("FilePath")) ? null : reader.GetString(reader.GetOrdinal("FilePath"))
                    };
                }

                List<DataProcessingDataOutput> dataOutputs = new();
                //Get List<DataProcessingDataOutput>
                reader.NextResult();
                while (await reader.ReadAsync())
                {
                    dataOutputs.Add(new DataProcessingDataOutput
                    {
                        AssessmentId = AssessmentId,
                        DataOutputId = reader.GetInt32(reader.GetOrdinal("DataOutputId")),
                        RecipientId = reader.GetInt32(reader.GetOrdinal("RecipientId")),
                        RecipientOfOutput = reader.IsDBNull(reader.GetOrdinal("RecipientOfOutput")) ? null : reader.GetString(reader.GetOrdinal("RecipientOfOutput")),
                        DataOutputAndUsageJustification = reader.IsDBNull(reader.GetOrdinal("DataOutputAndUsageJustification")) ? null : reader.GetString(reader.GetOrdinal("DataOutputAndUsageJustification"))
                    });
                }


                riskBenefitAssessment.DataOutputs = dataOutputs;

                List<UserNote> userNotes = new();
                //get List<UserNote>
                reader.NextResult();
                while (await reader.ReadAsync())
                {
                    userNotes.Add(new UserNote
                    {
                        AssessmentId = AssessmentId,
                        Note = reader.GetString(reader.GetOrdinal("Note")),
                        Decision = reader.GetString(reader.GetOrdinal("Decision")),
                        CreatedByRole = reader.GetInt32(reader.GetOrdinal("CreatedByRole")),
                        CreatedOn = reader.GetDateTime(reader.GetOrdinal("UpdatedOn"))

                    });
                }

                riskBenefitAssessment.PastDecisions = userNotes;


                _logger.LogInformation("Retrieved RiskBenefitAssessment metadata for AssessmentId {AssessmentId}", AssessmentId);

                // Continue with existing reader.NextResult() sections unchanged...
                // (omitted here for brevity)
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while fetching assessment {AssessmentId}", AssessmentId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching assessment {AssessmentId}", AssessmentId);
                throw;
            }

            return riskBenefitAssessment;
        }
    }
}
