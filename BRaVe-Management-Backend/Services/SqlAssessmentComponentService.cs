using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.Data.SqlClient;
namespace BRaVe_Management_Backend.Services
{
    public class SqlAssessmentComponentService: IAssessmentComponentService
    {
        private string connectionString { get; set; }
        public SqlAssessmentComponentService(ISecretProvider secretProvider)
        {

            connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection).Result;

        }
        //*************************************************************ADD DATA METHODS*************************************

        //UPDATE RISK BENEFIT ASSESSMENT STATUS
        public async Task UpdateRBAStatus(StatusChangeDto data, int TenantId, string UserId)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            UPDATE tbl_RiskBenefitAssessments
                SET StatusId = @ToStatusId, UpdatedByUserId=@UserId, UpdatedOn=GETUTCDATE()
                WHERE AssessmentId=@AssessmentId 
                AND StatusId=@FromStatusId
                AND TenantId=@TenantId 
                AND CreatedByUserId=@UserId;";

            cmd.Parameters.AddWithValue("@AssessmentId", data.AssessmentId);
            cmd.Parameters.AddWithValue("@TenantId", TenantId);
            cmd.Parameters.AddWithValue("@ToStatusId", data.ToStatus);
            cmd.Parameters.AddWithValue("@FromStatusId", data.FromStatus);
            cmd.Parameters.AddWithValue("@UserId", UserId);

            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                // log or bubble up the actual SQL error
                throw new Exception("Could not update Risk benefit assessment. " + ex.Message, ex);
            }
        }



        //ADD PERSONAL DATA
        public async Task AddPersonalData(DataProcessingPersonalData data, int TenantId, string UserId)
        {

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            INSERT INTO tbl_DataProcessingPersonalData
                (AssessmentId, TenantId,PersonalDataCategoryId, IsSpecialCategory, PurposeOfDataCollection, CreatedByUserId, CreatedOn,UpdatedByUserId, UpdatedOn)
            VALUES
                (@AssessmentId,  @TenantId,@PersonalDataCategoryId, @IsSpecialCategory, @PurposeOfDataCollection, @CreatedByUserId, GETDATE(), @CreatedByUserId, GETDATE());
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

            cmd.Parameters.AddWithValue("@AssessmentId", data.AssessmentId);
            cmd.Parameters.AddWithValue("@TenantId", TenantId);
            cmd.Parameters.AddWithValue("@PersonalDataCategoryId", data.PersonalDataCategoryId);
            cmd.Parameters.AddWithValue("@IsSpecialCategory", data.IsSpecialCategory);
            cmd.Parameters.AddWithValue("@PurposeOfDataCollection", (object?)data.PurposeOfDataCollection ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedByUserId", UserId);

            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                // log or bubble up the actual SQL error
                throw new Exception("Could not add Personal Data. " + ex.Message, ex);
            }


        }

        //DATA PROCESSING
        public async Task AddDataProcessing(DataProcessing data, int TenantId, string UserId)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            UPDATE tbl_DataProcessing
            SET DataManagerName =@DataManagerName
                  ,DataManagerContacts =@DataManagerContacts
                  ,PrimaryPurposeId=@PrimaryPurposeId
                  ,PrimaryPurposeOther=@PrimaryPurposeOther
                  ,SecondaryPurposeId=@SecondaryPurposeId
                  ,SecondaryPurposeOther=@SecondaryPurposeOther
                  ,UpdatedByUserId=@UpdatedByUserId
                  ,UpdatedOn=GETUTCDATE()
              WHERE AssessmentId=@AssessmentId AND TenantId=@TenantId;";

            cmd.Parameters.AddWithValue("@AssessmentId", data.AssessmentId);
            cmd.Parameters.AddWithValue("@TenantId", TenantId);
            cmd.Parameters.AddWithValue("@DataManagerName", data.DataManagerName);
            cmd.Parameters.AddWithValue("@DataManagerContacts", data.DataManagerContacts);
            cmd.Parameters.AddWithValue("@PrimaryPurposeId", data.PrimaryPurposeId);
            cmd.Parameters.AddWithValue("@PrimaryPurposeOther", (object?)data.PrimaryPurposeOther ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SecondaryPurposeId", data.SecondaryPurposeId);
            cmd.Parameters.AddWithValue("@SecondaryPurposeOther", (object?)data.SecondaryPurposeOther ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UpdatedByUserId", UserId);

            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                // log or bubble up the actual SQL error
                throw new Exception("Could not add Data Processing contact information. " + ex.Message, ex);
            }
        }

        //ADD LAWFUL BASIS
        public async Task AddLawfulBasis(DataProcessingLawfulBasis data, int TenantId, string UserId)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                MERGE tbl_DataProcessingLawfulBases AS tgt
                USING (
                    SELECT
                        @AssessmentId                        AS AssessmentId,
                        @TenantId                            AS TenantId,
                        @ProcessingActivityLawfulBasisId     AS ProcessingActivityLawfulBasisId,
                        @LawfulBasisExplanation              AS LawfulBasisExplanation,
                        @LawfulBasisDocPath                  AS LawfulBasisDocPath,
                        @InitialDataCollectionExplanation    AS InitialDataCollectionExplanation,
                        @InitialDataCollectionDocPath        AS InitialDataCollectionDocPath,
                        @OngoingDataManagementExplanation    AS OngoingDataManagementExplanation,
                        @OngoingDataManagementDocPath        AS OngoingDataManagementDocPath,
                        @DataSharingExplanation              AS DataSharingExplanation,
                        @DataSharingDocPath                  AS DataSharingDocPath
                ) AS src
                ON (tgt.AssessmentId = src.AssessmentId)

                WHEN MATCHED THEN
                    UPDATE SET
                        tgt.TenantId                          = src.TenantId,
                        tgt.ProcessingActivityLawfulBasisId    = src.ProcessingActivityLawfulBasisId,
                        tgt.LawfulBasisExplanation            = src.LawfulBasisExplanation,
                        tgt.LawfulBasisDocPath                = src.LawfulBasisDocPath,
                        tgt.InitialDataCollectionExplanation  = src.InitialDataCollectionExplanation,
                        tgt.InitialDataCollectionDocPath      = src.InitialDataCollectionDocPath,
                        tgt.OngoingDataManagementExplanation  = src.OngoingDataManagementExplanation,
                        tgt.OngoingDataManagementDocPath      = src.OngoingDataManagementDocPath,
                        tgt.DataSharingExplanation            = src.DataSharingExplanation,
                        tgt.DataSharingDocPath                = src.DataSharingDocPath,
                        tgt.UpdatedByUserId                   = @UserId,
                        tgt.UpdatedOn                         = GETUTCDATE()

                WHEN NOT MATCHED BY TARGET THEN
                    INSERT (
                        AssessmentId, TenantId, ProcessingActivityLawfulBasisId,
                        LawfulBasisExplanation, LawfulBasisDocPath,
                        InitialDataCollectionExplanation, InitialDataCollectionDocPath,
                        OngoingDataManagementExplanation, OngoingDataManagementDocPath,
                        DataSharingExplanation, DataSharingDocPath,
                        CreatedByUserId, CreatedOn, UpdatedByUserId, UpdatedOn
                    )
                    VALUES (
                        src.AssessmentId, src.TenantId, src.ProcessingActivityLawfulBasisId,
                        src.LawfulBasisExplanation, src.LawfulBasisDocPath,
                        src.InitialDataCollectionExplanation, src.InitialDataCollectionDocPath,
                        src.OngoingDataManagementExplanation, src.OngoingDataManagementDocPath,
                        src.DataSharingExplanation, src.DataSharingDocPath,
                        @UserId, GETUTCDATE(), @UserId, GETUTCDATE()
                    );";

            cmd.Parameters.AddWithValue("@AssessmentId", data.AssessmentId);
            cmd.Parameters.AddWithValue("@TenantId", TenantId);
            cmd.Parameters.AddWithValue("@ProcessingActivityLawfulBasisId", data.ProcessingActivityLawfulBasisId);

            cmd.Parameters.AddWithValue("@LawfulBasisExplanation", (object?)data.LawfulBasisExplanation ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@LawfulBasisDocPath", (object?)data.LawfulBasisDocPath ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@InitialDataCollectionExplanation", (object?)data.InitialDataCollectionExplanation ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@InitialDataCollectionDocPath", (object?)data.InitialDataCollectionDocPath ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@OngoingDataManagementExplanation", (object?)data.OngoingDataManagementExplanation ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@OngoingDataManagementDocPath", (object?)data.OngoingDataManagementDocPath ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@DataSharingExplanation", (object?)data.DataSharingExplanation ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DataSharingDocPath", (object?)data.DataSharingDocPath ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@UserId", UserId);



            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                throw new Exception("DatabaseError", ex);
            }
        }

        //ADD DATA PROCESSING RETENTION
        public async Task AddDataRetention(DataProcessingRetention data, int TenantId, string UserId)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"MERGE tbl_DataProcessingRetentions AS tgt
            USING (
                SELECT
                    @AssessmentId                                   AS AssessmentId,
                    @TenantId                                       AS TenantId,
                    @BeneficiaryDataYearsAfterProjectClosure        AS BeneficiaryDataYearsAfterProjectClosure,
                    @BeneficiaryDataMonthsAfterProjectClosure       AS BeneficiaryDataMonthsAfterProjectClosure,
                    @BeneficiaryDataRetentionJustification          AS BeneficiaryDataRetentionJustification,
                    @FinancialDataYearsAfterProjectClosure          AS FinancialDataYearsAfterProjectClosure,
                    @FinancialDataMonthsAfterProjectClosure         AS FinancialDataMonthsAfterProjectClosure,
                    @FinancialDataRetentionJustification            AS FinancialDataRetentionJustification,
                    @ConsentFormsYearsAfterProjectClosure           AS ConsentFormsYearsAfterProjectClosure,
                    @ConsentFormsMonthsAfterProjectClosure          AS ConsentFormsMonthsAfterProjectClosure,
                    @ConsentFormsRetentionJustification             AS ConsentFormsRetentionJustification,
                    @MethodOfArchivingAfterProjectClosure           AS MethodOfArchivingAfterProjectClosure,
                    @MethodOfArchivingAfterProjectClosureOther      AS MethodOfArchivingAfterProjectClosureOther,
                    @MethodOfDisposalAfterRetentionPeriod           AS MethodOfDisposalAfterRetentionPeriod,
                    @MethodOfDisposalAfterRetentionPeriodOther      AS MethodOfDisposalAfterRetentionPeriodOther
            ) AS src
            ON (tgt.AssessmentId = src.AssessmentId)

            WHEN MATCHED THEN
                UPDATE SET
                    tgt.TenantId                                  = src.TenantId,
                    tgt.BeneficiaryDataYearsAfterProjectClosure   = src.BeneficiaryDataYearsAfterProjectClosure,
                    tgt.BeneficiaryDataMonthsAfterProjectClosure  = src.BeneficiaryDataMonthsAfterProjectClosure,
                    tgt.BeneficiaryDataRetentionJustification     = src.BeneficiaryDataRetentionJustification,
                    tgt.FinancialDataYearsAfterProjectClosure     = src.FinancialDataYearsAfterProjectClosure,
                    tgt.FinancialDataMonthsAfterProjectClosure    = src.FinancialDataMonthsAfterProjectClosure,
                    tgt.FinancialDataRetentionJustification       = src.FinancialDataRetentionJustification,
                    tgt.ConsentFormsYearsAfterProjectClosure      = src.ConsentFormsYearsAfterProjectClosure,
                    tgt.ConsentFormsMonthsAfterProjectClosure     = src.ConsentFormsMonthsAfterProjectClosure,
                    tgt.ConsentFormsRetentionJustification        = src.ConsentFormsRetentionJustification,
                    tgt.MethodOfArchivingAfterProjectClosure      = src.MethodOfArchivingAfterProjectClosure,
                    tgt.MethodOfArchivingAfterProjectClosureOther = src.MethodOfArchivingAfterProjectClosureOther,
                    tgt.MethodOfDisposalAfterRetentionPeriod      = src.MethodOfDisposalAfterRetentionPeriod,
                    tgt.MethodOfDisposalAfterRetentionPeriodOther = src.MethodOfDisposalAfterRetentionPeriodOther,
                    tgt.UpdatedByUserId                           = @UserId,
                    tgt.UpdatedOn                                 = GETUTCDATE()

            WHEN NOT MATCHED BY TARGET THEN
                INSERT (
                    AssessmentId, TenantId,
                    BeneficiaryDataYearsAfterProjectClosure, BeneficiaryDataMonthsAfterProjectClosure,
                    BeneficiaryDataRetentionJustification,
                    FinancialDataYearsAfterProjectClosure, FinancialDataMonthsAfterProjectClosure,
                    FinancialDataRetentionJustification,
                    ConsentFormsYearsAfterProjectClosure, ConsentFormsMonthsAfterProjectClosure,
                    ConsentFormsRetentionJustification,
                    MethodOfArchivingAfterProjectClosure, MethodOfArchivingAfterProjectClosureOther,
                    MethodOfDisposalAfterRetentionPeriod, MethodOfDisposalAfterRetentionPeriodOther,
                    CreatedByUserId, CreatedOn, UpdatedByUserId, UpdatedOn
                )
                VALUES (
                    src.AssessmentId, src.TenantId,
                    src.BeneficiaryDataYearsAfterProjectClosure, src.BeneficiaryDataMonthsAfterProjectClosure,
                    src.BeneficiaryDataRetentionJustification,
                    src.FinancialDataYearsAfterProjectClosure, src.FinancialDataMonthsAfterProjectClosure,
                    src.FinancialDataRetentionJustification,
                    src.ConsentFormsYearsAfterProjectClosure, src.ConsentFormsMonthsAfterProjectClosure,
                    src.ConsentFormsRetentionJustification,
                    src.MethodOfArchivingAfterProjectClosure, src.MethodOfArchivingAfterProjectClosureOther,
                    src.MethodOfDisposalAfterRetentionPeriod, src.MethodOfDisposalAfterRetentionPeriodOther,
                    @UserId, GETUTCDATE(), @UserId, GETUTCDATE()
                );";

            cmd.Parameters.AddWithValue("@AssessmentId", data.AssessmentId);
            cmd.Parameters.AddWithValue("@TenantId", TenantId);

            // Nullable ints
            cmd.Parameters.AddWithValue("@BeneficiaryDataYearsAfterProjectClosure",
                (object?)data.BeneficiaryDataYearsAfterProjectClosure ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BeneficiaryDataMonthsAfterProjectClosure",
                (object?)data.BeneficiaryDataMonthsAfterProjectClosure ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FinancialDataYearsAfterProjectClosure",
                (object?)data.FinancialDataYearsAfterProjectClosure ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FinancialDataMonthsAfterProjectClosure",
                (object?)data.FinancialDataMonthsAfterProjectClosure ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ConsentFormsYearsAfterProjectClosure",
                (object?)data.ConsentFormsYearsAfterProjectClosure ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ConsentFormsMonthsAfterProjectClosure",
                (object?)data.ConsentFormsMonthsAfterProjectClosure ?? DBNull.Value);

            // Required NVARCHAR(500) strings (ensure non-null in your model)
            cmd.Parameters.AddWithValue("@BeneficiaryDataRetentionJustification", data.BeneficiaryDataRetentionJustification);
            cmd.Parameters.AddWithValue("@FinancialDataRetentionJustification", data.FinancialDataRetentionJustification);
            cmd.Parameters.AddWithValue("@ConsentFormsRetentionJustification", data.ConsentFormsRetentionJustification);

            // Required ints
            cmd.Parameters.AddWithValue("@MethodOfArchivingAfterProjectClosure", data.MethodOfArchivingAfterProjectClosure);
            cmd.Parameters.AddWithValue("@MethodOfDisposalAfterRetentionPeriod", data.MethodOfDisposalAfterRetentionPeriod);

            // Required NVARCHAR(500) "Other" strings (ensure non-null)
            cmd.Parameters.AddWithValue("@MethodOfArchivingAfterProjectClosureOther", data.MethodOfArchivingAfterProjectClosureOther);
            cmd.Parameters.AddWithValue("@MethodOfDisposalAfterRetentionPeriodOther", data.MethodOfDisposalAfterRetentionPeriodOther);

            // Audit
            cmd.Parameters.AddWithValue("@UserId", UserId);


            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                throw new Exception("DatabaseError", ex);
            }
        }

        //ADD COLLABORATORS
        public async Task AddCollaborator(DataProcessingCollaborator data, int TenantId, string UserId)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        INSERT INTO tbl_DataProcessingCollaborators
            (AssessmentId, TenantId,PartnerId, PartnerOrganizationName, PartnerOrganizationContacts, description,
             CreatedByUserId, CreatedOn, UpdatedByUserId, UpdatedOn,FilePath)
        VALUES
            (@AssessmentId, @TenantId, @PartnerId, @PartnerOrganizationName, @PartnerOrganizationContacts, @Description,
             @CreatedByUserId, GETDATE(), @CreatedByUserId, GETDATE(),@FilePath);
        SELECT CAST(SCOPE_IDENTITY() AS INT);";

            cmd.Parameters.AddWithValue("@AssessmentId", data.AssessmentId);
            cmd.Parameters.AddWithValue("@TenantId", TenantId);
            cmd.Parameters.AddWithValue("@PartnerId", data.PartnerId);
            cmd.Parameters.AddWithValue("@PartnerOrganizationName", data.PartnerOrganizationName);
            cmd.Parameters.AddWithValue("@PartnerOrganizationContacts", (object?)data.PartnerOrganizationContacts ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Description", data.Description ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedByUserId", UserId);
            cmd.Parameters.AddWithValue("@FilePath", (object?)data.FilePath ?? DBNull.Value);

            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                // Duplicate partner for the same assessment
                throw new Exception("DuplicatePartner", ex);
            }
            catch (SqlException ex)
            {
                throw new Exception("DatabaseError", ex);
            }
        }


        //ADD DATA SUBJECTS
        public async Task AddDataSubjects(DataProcessingDataSubject data, int TenantId, string UserId)
        {

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            INSERT INTO tbl_DataProcessingDataSubjects
                (AssessmentId, TenantId,SubjectTypeId, IsVunerableGroup,CreatedByUserId, CreatedOn,UpdatedByUserId, UpdatedOn)
            VALUES
                (@AssessmentId, @TenantId, @SubjectTypeId, @IsVunerableGroup,  @CreatedByUserId, GETDATE(), @CreatedByUserId, GETDATE());
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

            cmd.Parameters.AddWithValue("@AssessmentId", data.AssessmentId);
            cmd.Parameters.AddWithValue("@TenantId", TenantId);
            cmd.Parameters.AddWithValue("@SubjectTypeId", data.SubjectTypeId);
            cmd.Parameters.AddWithValue("@IsVunerableGroup", data.IsVulnerableGroup);
            cmd.Parameters.AddWithValue("@CreatedByUserId", UserId);

            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                // log or bubble up the actual SQL error
                throw new Exception("Could not add Personal Data. " + ex.Message, ex);
            }


        }


        //ADD DATA DISCLOSURE RECEPIENTS
        public async Task AddDataDisclosureRecipients(DataProcessingDataDisclosureRecipient data, int TenantId, string UserId)
        {

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            INSERT INTO tbl_DataProcessingDataDisclosureRecipients
                (AssessmentId, TenantId, RecipientId, RecipientOther, NameOfRecipientDepartment , PurposeOfDataDisclosure, IsDataAggregatedBeforeSharing,IsDataAnonymizedBeforeSharing,PurposeOfDataAccess,CreatedByUserId, CreatedOn,UpdatedByUserId, UpdatedOn)
            VALUES
                (@AssessmentId, @TenantId, @RecipientId, @RecipientOther, @NameOfRecipientDepartment,@PurposeOfDataDisclosure,@IsDataAggregatedBeforeSharing,@IsDataAnonymizedBeforeSharing,@PurposeOfDataAccess, @CreatedByUserId, GETDATE(), @CreatedByUserId, GETDATE());
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

            cmd.Parameters.AddWithValue("@AssessmentId", data.AssessmentId);
            cmd.Parameters.AddWithValue("@TenantId", TenantId);
            cmd.Parameters.AddWithValue("@RecipientId", data.RecipientId);
            cmd.Parameters.AddWithValue("@RecipientOther",(object?)data.RecipientOther ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@NameOfRecipientDepartment", data.NameOfRecipientDepartment);
            cmd.Parameters.AddWithValue("@PurposeOfDataDisclosure", data.PurposeOfDataDisclosure);
            cmd.Parameters.AddWithValue("@IsDataAggregatedBeforeSharing", data.IsDataAggregatedBeforeSharing);
            cmd.Parameters.AddWithValue("@IsDataAnonymizedBeforeSharing", data.IsDataAnonymizedBeforeSharing);
            cmd.Parameters.AddWithValue("@PurposeOfDataAccess", data.PurposeOfDataAccess);
            cmd.Parameters.AddWithValue("@CreatedByUserId", UserId);

            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                // log or bubble up the actual SQL error
                throw new Exception("Could not add Data disclosure recipient. " + ex.Message, ex);
            }


        }

        //ADD DATA SHARING RECEPIENTS
        public async Task AddDataSharingRecipients(DataProcessingDataSharingRecipient data, int TenantId, string UserId)
        {

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            INSERT INTO tbl_DataProcessingDataSharingRecipients
                (AssessmentId, TenantId,RecipientId, RecipientOther, TypeOfAgreementId , TypeOfAgreementOther, StatusOfAgreementId,PurposeOfDataSharing,CreatedByUserId, CreatedOn,UpdatedByUserId, UpdatedOn)
            VALUES
                (@AssessmentId, @TenantId, @RecipientId, @RecipientOther, @TypeOfAgreementId,@TypeOfAgreementOther,@StatusOfAgreementId,@PurposeOfDataSharing, @CreatedByUserId, GETDATE(), @CreatedByUserId, GETDATE());
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

            cmd.Parameters.AddWithValue("@AssessmentId", data.AssessmentId);
            cmd.Parameters.AddWithValue("@TenantId", TenantId);
            cmd.Parameters.AddWithValue("@RecipientId", data.RecipientId);
            cmd.Parameters.AddWithValue("@RecipientOther", data.RecipientOther);
            cmd.Parameters.AddWithValue("@TypeOfAgreementId", data.TypeOfAgreementId);
            cmd.Parameters.AddWithValue("@TypeOfAgreementOther", (object?)data.TypeOfAgreementOther ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@StatusOfAgreementId", data.StatusOfAgreementId);
            cmd.Parameters.AddWithValue("@PurposeOfDataSharing", data.PurposeOfDataSharing);
            cmd.Parameters.AddWithValue("@CreatedByUserId", UserId);

            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                // log or bubble up the actual SQL error
                throw new Exception("Could not add Data sharing recepient. " + ex.Message, ex);
            }


        }

        //ADD DATA SECURITY MEASURES
        public async Task AddSecurityMeasures(DataProcessingSecurityMeasure data, int TenantId, string UserId)
        {

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            INSERT INTO tbl_DataProcessingSecurityMeasures
                (AssessmentId, TenantId,SecurityMeasureId, TechnicalMeasures, OrganizationalMeasures ,CreatedByUserId, CreatedOn,UpdatedByUserId, UpdatedOn)
            VALUES
                (@AssessmentId, @TenantId, @SecurityMeasureId, @TechnicalMeasures, @OrganizationalMeasures, @CreatedByUserId, GETDATE(), @CreatedByUserId, GETDATE());
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

            cmd.Parameters.AddWithValue("@AssessmentId", data.AssessmentId);
            cmd.Parameters.AddWithValue("@TenantId", TenantId);
            cmd.Parameters.AddWithValue("@SecurityMeasureId", data.SecurityMeasureId);
            cmd.Parameters.AddWithValue("@TechnicalMeasures", data.TechnicalMeasures);
            cmd.Parameters.AddWithValue("@OrganizationalMeasures", data.OrganizationalMeasures);
            cmd.Parameters.AddWithValue("@CreatedByUserId", UserId);

            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                // log or bubble up the actual SQL error
                throw new Exception("Could not add Security Measures. " + ex.Message, ex);
            }


        }


        //ADD SOURCES OF DATA
        public async Task AddSourcesOfData(DataProcessingSourceOfData data, int TenantId, string UserId)
        {

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            INSERT INTO tbl_DataProcessingSourceOfData
                (AssessmentId, TenantId,PrimarySourceId, PrimarySourceOther, SecondarySourceId ,SecondarySourceOther,CreatedByUserId, CreatedOn,UpdatedByUserId, UpdatedOn,FilePath)
            VALUES
                (@AssessmentId, @TenantId, @PrimarySourceId, @PrimarySourceOther, @SecondarySourceId, @SecondarySourceOther,@CreatedByUserId, GETDATE(), @CreatedByUserId, GETDATE(),@FilePath);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

            cmd.Parameters.AddWithValue("@AssessmentId", data.AssessmentId);
            cmd.Parameters.AddWithValue("@TenantId", TenantId);
            cmd.Parameters.AddWithValue("@PrimarySourceId", data.PrimarySourceId);
            cmd.Parameters.AddWithValue("@PrimarySourceOther", (object?)data.PrimarySourceOther ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SecondarySourceId", data.SecondarySourceId);
            cmd.Parameters.AddWithValue("@SecondarySourceOther", (object?)data.SecondarySourceOther ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedByUserId", UserId);
            cmd.Parameters.AddWithValue("@FilePath", (object?)data.AttachmentUrl ?? DBNull.Value);

            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                // log or bubble up the actual SQL error
                throw new Exception("Could not add Source of data. " + ex.Message, ex);
            }


        }

        //ADD DATA OUTPUTS
        public async Task AddDataOutputs(DataProcessingDataOutput data, int TenantId, string UserId)
        {

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            INSERT INTO tbl_DataProcessingDataOutputs
                (AssessmentId, TenantId,DataOutputId, RecipientId, RecipientOfOutput ,DataOutputAndUsageJustification,CreatedByUserId, CreatedOn,UpdatedByUserId, UpdatedOn)
            VALUES
                (@AssessmentId, @TenantId, @DataOutputId, @RecipientId, @RecipientOfOutput, @DataOutputAndUsageJustification,@CreatedByUserId, GETDATE(), @CreatedByUserId, GETDATE());
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

            cmd.Parameters.AddWithValue("@AssessmentId", data.AssessmentId);
            cmd.Parameters.AddWithValue("@TenantId", TenantId);
            cmd.Parameters.AddWithValue("@DataOutputId", data.DataOutputId);
            cmd.Parameters.AddWithValue("@RecipientId", data.RecipientId);
            cmd.Parameters.AddWithValue("@RecipientOfOutput", data.RecipientOfOutput);
            cmd.Parameters.AddWithValue("@DataOutputAndUsageJustification", data.DataOutputAndUsageJustification);
            cmd.Parameters.AddWithValue("@CreatedByUserId", UserId);

            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                // log or bubble up the actual SQL error
                throw new Exception("Could not add data output. " + ex.Message, ex);
            }

        }

        //*****************************************************  DELETE METHODS********************************************************************************************

        // DELETE COLLABORATOR
        public async Task DeleteCollaborator(int assessmentId, int partnerId, string userId)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        DELETE FROM tbl_DataProcessingCollaborators
        WHERE AssessmentId = @AssessmentId AND PartnerId = @PartnerId;";

            cmd.Parameters.AddWithValue("@AssessmentId", assessmentId);
            cmd.Parameters.AddWithValue("@PartnerId", partnerId);

            try
            {
                var rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0)
                {
                    throw new Exception("CollaboratorNotFound");
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("DatabaseError", ex);
            }
        }

        // DELETE DATA SUBJECT
        public async Task DeleteDataSubjects(int assessmentId, int subjectId, string userId)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        DELETE FROM tbl_DataProcessingDataSubjects
        WHERE AssessmentId = @AssessmentId AND SubjectTypeId = @subjectId;";

            cmd.Parameters.AddWithValue("@AssessmentId", assessmentId);
            cmd.Parameters.AddWithValue("@subjectId", subjectId);

            try
            {
                var rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0)
                {
                    throw new Exception("CollaboratorNotFound");
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("DatabaseError", ex);
            }
        }


        // DELETE PERSONAL DATA
        public async Task DeletePersonalData(int assessmentId, int personalcategoryId, string userId)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        DELETE FROM tbl_DataProcessingPersonalData
        WHERE AssessmentId = @AssessmentId AND PersonalDataCategoryId = @personalcategoryId;";

            cmd.Parameters.AddWithValue("@AssessmentId", assessmentId);
            cmd.Parameters.AddWithValue("@personalcategoryId", personalcategoryId);

            try
            {
                var rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0)
                {
                    throw new Exception("CollaboratorNotFound");
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("DatabaseError", ex);
            }
        }

        // DELETE DISCLOSURE RECIPIENTS
        public async Task DeleteDisclosureRecipients(int assessmentId, int recipientId, string userId)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        DELETE FROM tbl_DataProcessingDataDisclosureRecipients
        WHERE AssessmentId = @AssessmentId AND RecipientId = @recipientId;";

            cmd.Parameters.AddWithValue("@AssessmentId", assessmentId);
            cmd.Parameters.AddWithValue("@recipientId", recipientId);

            try
            {
                var rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0)
                {
                    throw new Exception("CollaboratorNotFound");
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("DatabaseError", ex);
            }
        }

        // DELETE SHARING RECIPIENTS
        public async Task DeleteSharingRecipients(int assessmentId, int recipientId, string userId)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        DELETE FROM tbl_DataProcessingDataSharingRecipients
        WHERE AssessmentId = @AssessmentId AND RecipientId = @recipientId;";

            cmd.Parameters.AddWithValue("@AssessmentId", assessmentId);
            cmd.Parameters.AddWithValue("@recipientId", recipientId);

            try
            {
                var rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0)
                {
                    throw new Exception("CollaboratorNotFound");
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("DatabaseError", ex);
            }
        }

        // DELETE SECURITY MEASURES
        public async Task DeleteSecurityMeasures(int assessmentId, int measureId, string userId)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        DELETE FROM tbl_DataProcessingSecurityMeasures
        WHERE AssessmentId = @AssessmentId AND SecurityMeasureId = @measureId;";

            cmd.Parameters.AddWithValue("@AssessmentId", assessmentId);
            cmd.Parameters.AddWithValue("@measureId", measureId);

            try
            {
                var rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0)
                {
                    throw new Exception("CollaboratorNotFound");
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("DatabaseError", ex);
            }
        }


        // DELETE SOURCES OF DATA
        public async Task DeleteSourcesOfData(int assessmentId, string userId)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        DELETE FROM tbl_DataProcessingSourceOfData
        WHERE AssessmentId = @AssessmentId;";

            cmd.Parameters.AddWithValue("@AssessmentId", assessmentId);

            try
            {
                var rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0)
                {
                    throw new Exception("CollaboratorNotFound");
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("DatabaseError", ex);
            }
        }

        // DELETE DATA OUTPUTS
        public async Task DeleteDataOutputs(int assessmentId, int outputId, string userId)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        DELETE FROM tbl_DataProcessingDataOutputs
        WHERE AssessmentId = @AssessmentId AND DataOutputId = @outputId;";

            cmd.Parameters.AddWithValue("@AssessmentId", assessmentId);
            cmd.Parameters.AddWithValue("@outputId", outputId);

            try
            {
                var rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0)
                {
                    throw new Exception("CollaboratorNotFound");
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("DatabaseError", ex);
            }
        }





        //*****************************************************  EDIT DATA METHODS********************************************************************************************

        //EDIT COLLABORATOR

        public async Task EditCollaborator(DataProcessingCollaborator data, string userId)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        UPDATE tbl_DataProcessingCollaborators
        SET PartnerOrganizationName=@PartnerOrganizationName, PartnerOrganizationContacts=@PartnerOrganizationContacts, description=@description,
              UpdatedByUserId=@UserId, UpdatedOn=GETDATE(),FilePath=@FilePath
        WHERE AssessmentId = @AssessmentId AND PartnerId = @PartnerId;";

            cmd.Parameters.AddWithValue("@AssessmentId", data.AssessmentId);
            cmd.Parameters.AddWithValue("@PartnerId", data.PartnerId);
            cmd.Parameters.AddWithValue("@PartnerOrganizationName", data.PartnerOrganizationName);
            cmd.Parameters.AddWithValue("@PartnerOrganizationContacts", (object?)data.PartnerOrganizationContacts ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Description", data.Description ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@FilePath", (object?)data.FilePath ?? DBNull.Value);

            try
            {
                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    throw new Exception("CollaboratorNotFound");
                }
            }
         
            catch (SqlException ex)
            {
                throw new Exception("DatabaseError", ex);
            }
        }


        //EDIT DATA SUBJECT

        public async Task EditDataSubjects(DataProcessingDataSubject data, string userId)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        UPDATE tbl_DataProcessingDataSubjects
        SET SubjectTypeId=@SubjectTypeId, IsVunerableGroup=@IsVunerableGroup,
              UpdatedByUserId=@UserId, UpdatedOn=GETDATE()
        WHERE AssessmentId = @AssessmentId AND SubjectTypeId = @SubjectTypeId;";

            cmd.Parameters.AddWithValue("@AssessmentId", data.AssessmentId);
            cmd.Parameters.AddWithValue("@SubjectTypeId", data.SubjectTypeId);
            cmd.Parameters.AddWithValue("@IsVunerableGroup", data.IsVulnerableGroup);
            cmd.Parameters.AddWithValue("@UserId", userId);


            try
            {
                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    throw new Exception("Data Subject NotFound");
                }
            }

            catch (SqlException ex)
            {
                throw new Exception("DatabaseError", ex);
            }
        }


        //EDIT PERSONAL DATA

        public async Task EditPersonalData(DataProcessingPersonalData data, string userId)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        UPDATE tbl_DataProcessingPersonalData
        SET PersonalDataCategoryId=@PersonalDataCategoryId, IsSpecialCategory=@IsSpecialCategory, PurposeOfDataCollection=@PurposeOfDataCollection,
              UpdatedByUserId=@UserId, UpdatedOn=GETDATE()
        WHERE AssessmentId = @AssessmentId AND PersonalDataCategoryId = @PersonalDataCategoryId;";

            cmd.Parameters.AddWithValue("@AssessmentId", data.AssessmentId);
            cmd.Parameters.AddWithValue("@PersonalDataCategoryId", data.PersonalDataCategoryId);
            cmd.Parameters.AddWithValue("@IsSpecialCategory", data.IsSpecialCategory);
            cmd.Parameters.AddWithValue("@PurposeOfDataCollection", (object?)data.PurposeOfDataCollection ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UserId", userId);

            try
            {
                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    throw new Exception("PersonalDataNotFound");
                }
            }

            catch (SqlException ex)
            {
                throw new Exception("DatabaseError", ex);
            }
        }


        //EDIT DisclosureRecipients

        public async Task EditDisclosureRecipients(DataProcessingDataDisclosureRecipient data, string userId)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        UPDATE tbl_DataProcessingDataDisclosureRecipients
        SET  RecipientOther=@RecipientOther,NameOfRecipientDepartment=@NameOfRecipientDepartment, PurposeOfDataAccess=@PurposeOfDataAccess,IsDataAggregatedBeforeSharing=@IsDataAggregatedBeforeSharing,
        IsDataAnonymizedBeforeSharing=@IsDataAnonymizedBeforeSharing,PurposeOfDataDisclosure=@PurposeOfDataDisclosure,
              UpdatedByUserId=@UserId, UpdatedOn=GETDATE()
        WHERE AssessmentId = @AssessmentId AND RecipientId = @RecipientId;";

            cmd.Parameters.AddWithValue("@AssessmentId", data.AssessmentId);
            cmd.Parameters.AddWithValue("@RecipientId", data.RecipientId);
            cmd.Parameters.AddWithValue("@RecipientOther", (object?)data.RecipientOther ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@NameOfRecipientDepartment", data.NameOfRecipientDepartment);
            cmd.Parameters.AddWithValue("@PurposeOfDataDisclosure", (object?)data.PurposeOfDataDisclosure ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsDataAggregatedBeforeSharing", data.IsDataAggregatedBeforeSharing);
            cmd.Parameters.AddWithValue("@IsDataAnonymizedBeforeSharing", data.IsDataAnonymizedBeforeSharing);
            cmd.Parameters.AddWithValue("@PurposeOfDataAccess", data.PurposeOfDataAccess);
            cmd.Parameters.AddWithValue("@UserId", userId);

            try
            {
                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    throw new Exception("DisclosureRecipientsNotFound");
                }
            }

            catch (SqlException ex)
            {
                throw new Exception("DatabaseError", ex);
            }
        }


        //EDIT SHARING RECIPIENTS

        public async Task EditSharingRecipients(DataProcessingDataSharingRecipient data, string userId)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        UPDATE tbl_DataProcessingDataSharingRecipients
        SET RecipientOther=@RecipientOther, TypeOfAgreementId=@TypeOfAgreementId, TypeOfAgreementOther=@TypeOfAgreementOther,StatusOfAgreementId=@StatusOfAgreementId,
PurposeOfDataSharing=@PurposeOfDataSharing,
              UpdatedByUserId=@UserId, UpdatedOn=GETDATE()
        WHERE AssessmentId = @AssessmentId AND RecipientId = @RecipientId;";

            cmd.Parameters.AddWithValue("@AssessmentId", data.AssessmentId);
            cmd.Parameters.AddWithValue("@RecipientId", data.RecipientId);
            cmd.Parameters.AddWithValue("@RecipientOther", (object?)data.RecipientOther ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@TypeOfAgreementId", data.TypeOfAgreementId);
            cmd.Parameters.AddWithValue("@TypeOfAgreementOther", (object?)data.TypeOfAgreementOther ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@StatusOfAgreementId", data.StatusOfAgreementId);
            cmd.Parameters.AddWithValue("@PurposeOfDataSharing", data.PurposeOfDataSharing);
            cmd.Parameters.AddWithValue("@UserId", userId);

            try
            {
                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    throw new Exception("SharingRecipientNotFound");
                }
            }

            catch (SqlException ex) when (ex.Number == 547)
            {
                if (ex.Message.Contains("check constraint", StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new Exception("CheckConstraintError", ex);
                }

                throw new Exception("DatabaseError", ex);
            }
        }


        //EDIT SECURITY MEASURES

        public async Task EditSecurityMeasures(DataProcessingSecurityMeasure data, string userId)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        UPDATE tbl_DataProcessingSecurityMeasures
        SET TechnicalMeasures=@TechnicalMeasures, OrganizationalMeasures=@OrganizationalMeasures,
              UpdatedByUserId=@UserId, UpdatedOn=GETDATE()
        WHERE AssessmentId = @AssessmentId AND SecurityMeasureId = @SecurityMeasureId;";

            cmd.Parameters.AddWithValue("@AssessmentId", data.AssessmentId);
            cmd.Parameters.AddWithValue("@SecurityMeasureId", data.SecurityMeasureId);
            cmd.Parameters.AddWithValue("@TechnicalMeasures", data.TechnicalMeasures);
            cmd.Parameters.AddWithValue("@OrganizationalMeasures", data.OrganizationalMeasures);
            cmd.Parameters.AddWithValue("@UserId", userId);

            try
            {
                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    throw new Exception("SecurityMeasurerNotFound");
                }
            }

            catch (SqlException ex)
            {
                throw new Exception("DatabaseError", ex);
            }
        }


        //EDIT SOURCES OF DATA

        public async Task EditSourcesOfData(DataProcessingSourceOfData data, string userId)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                    UPDATE tbl_DataProcessingSourceOfData
                    SET PrimarySourceId = @PrimarySourceId, PrimarySourceOther=@PrimarySourceOther, SecondarySourceId=@SecondarySourceId, SecondarySourceOther=@SecondarySourceOther,
                          UpdatedByUserId=@UserId, UpdatedOn=GETDATE(),FilePath=@FilePath
                    WHERE AssessmentId = @AssessmentId;";

            cmd.Parameters.AddWithValue("@AssessmentId", data.AssessmentId);
            
            cmd.Parameters.AddWithValue("@PrimarySourceId", data.PrimarySourceId);
            cmd.Parameters.AddWithValue("@PrimarySourceOther", (object?)data.PrimarySourceOther ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SecondarySourceId", data.SecondarySourceId);
            cmd.Parameters.AddWithValue("@SecondarySourceOther", (object?)data.SecondarySourceOther ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FilePath", data.AttachmentUrl);
            cmd.Parameters.AddWithValue("@UserId", userId);

            try
            {
                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    throw new Exception("SourcesofdataNotFound");
                }
            }

            catch (SqlException ex)
            {
                throw new Exception("DatabaseError", ex);
            }
        }


        //EDIT DATA OUTPUTS

        public async Task EditDataOutputs(DataProcessingDataOutput data, string userId)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        UPDATE tbl_DataProcessingDataOutputs
        SET RecipientOfOutput=@RecipientOfOutput, DataOutputAndUsageJustification=@DataOutputAndUsageJustification, RecipientId=@RecipientId,
              UpdatedByUserId=@UserId, UpdatedOn=GETDATE()
        WHERE AssessmentId = @AssessmentId AND DataOutputId = @DataOutputId;";

            cmd.Parameters.AddWithValue("@AssessmentId", data.AssessmentId);
            cmd.Parameters.AddWithValue("@DataOutputId", data.DataOutputId);
            cmd.Parameters.AddWithValue("@RecipientOfOutput", data.RecipientOfOutput);
            cmd.Parameters.AddWithValue("@DataOutputAndUsageJustification", data.DataOutputAndUsageJustification);
            cmd.Parameters.AddWithValue("@RecipientId", data.RecipientId);
            cmd.Parameters.AddWithValue("@UserId", userId);

            try
            {
                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    throw new Exception("DataOutputsNotFound");
                }
            }

            catch (SqlException ex)
            {
                throw new Exception("DatabaseError", ex);
            }
        }

        
    }
}
