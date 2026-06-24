using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BRaVe_Management_Backend.Services
{
    public class SqlConsentService : IConsentService
    {
        private readonly string connectionString;
        private readonly ILogger<SqlConsentService> _logger;

        public SqlConsentService(
            ISecretProvider secretProvider,
            ILogger<SqlConsentService> logger)
        {
            _logger = logger;

            connectionString = secretProvider
                .GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection)
                .Result;
        }

        public async Task CreateConsent(
            ConsentDto data,
            int tenantId,
            string userId)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);

                await conn.OpenAsync();

                using var cmd = new SqlCommand("sp_CreateConsent", conn);

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@TenantId", tenantId);

                cmd.Parameters.AddWithValue("@ProgramId", data.ProgramId);

                cmd.Parameters.AddWithValue(
                    "@Description",
                    (object?)data.Description ?? DBNull.Value);

                cmd.Parameters.AddWithValue(
                    "@Title",
                    (object?)data.Title ?? DBNull.Value);

                cmd.Parameters.AddWithValue("@IsActive", data.IsActive);

                cmd.Parameters.AddWithValue("@CreatedByUserId", userId);

                cmd.Parameters.AddWithValue(
                    "@DefaultLang",
                    (object?)data.DefaultLang ?? "en");

                await cmd.ExecuteNonQueryAsync();

                _logger.LogInformation(
                    "Consent created successfully for ProgramId {ProgramId} by User {UserId}",
                    data.ProgramId,
                    userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating consent for ProgramId {ProgramId}",
                    data.ProgramId);

                throw;
            }
        }

        public async Task UpdateConsent(int id, ConsentDto data, string userId, string languageCode)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("sp_UpdateConsent", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@ProgramId", data.ProgramId);
                cmd.Parameters.AddWithValue("@LanguageCode", languageCode);

                cmd.Parameters.AddWithValue("@Title", (object?)data.Title ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Description", (object?)data.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IsActive", data.IsActive);
                cmd.Parameters.AddWithValue("@UpdatedByUserId", userId);

                await cmd.ExecuteNonQueryAsync();

                _logger.LogInformation(
                    "Consent updated successfully. ConsentId={ConsentId}, ProgramId={ProgramId}, LanguageCode={LanguageCode}",
                    id,
                    data.ProgramId,
                    languageCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error updating consent with Id={ConsentId}, ProgramId={ProgramId}, LanguageCode={LanguageCode}",
                    id,
                    data.ProgramId,
                    languageCode);

                throw;
            }
        }
        public async Task<IEnumerable<Consent>> GetAllConsents(int ProgramId)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();


                using var cmd = new SqlCommand("sp_GetConsentsByProgramId", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@ProgramId", ProgramId);
                cmd.Parameters.AddWithValue("@LanguageCode", "en");


                List<Consent> Consents = new List<Consent>();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var consent = new Consent()
                        {

                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Description = reader.GetString(reader.GetOrdinal("Description")),
                            Title = reader.GetString(reader.GetOrdinal("Title")),
                            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                            ProgramId = ProgramId,

                            CreatedOn = reader.GetDateTime(reader.GetOrdinal("CreatedOn")),

                            UpdatedOn = reader.IsDBNull(reader.GetOrdinal("UpdatedOn")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedOn"))
                            //UpdatedOn = reader.GetDateTime(reader.GetOrdinal("UpdatedOn")),

                            // EnumeratorPin = reader.IsDBNull(reader.GetOrdinal("EnumeratorPin")) ? null : reader.GetString(reader.GetOrdinal("FullName")),
                        };
                        Consents.Add(consent);

                    }
                }

                return Consents;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        /*public Task<Location?> GetLocationById(int id)
        {
            throw new NotImplementedException();
        }*/

        public async Task<Consent?> GetConsentById(int id)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("sp_GetConsentById", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Id", id);

                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                    return null;

                return new Consent
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    TenantId = reader.GetInt32(reader.GetOrdinal("TenantId")),
                    ProgramId = reader.GetInt32(reader.GetOrdinal("ProgramId")),

                    Title = reader.IsDBNull(reader.GetOrdinal("Title"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("Title")),

                    Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("Description")),

                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),

                    CreatedOn = reader.GetDateTime(reader.GetOrdinal("CreatedOn")),

                    UpdatedOn = reader.IsDBNull(reader.GetOrdinal("UpdatedOn"))
                        ? (DateTime?)null
                        : reader.GetDateTime(reader.GetOrdinal("UpdatedOn"))
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving consent by Id {ConsentId}", id);
                throw;
            }
        }

        /*public Task GetConsentByConsentId(int consentId)
        {
            throw new NotImplementedException();
        }*/

        public async Task AddOrUpdateTranslationAsync(
            ConsentTranslationDto translation,
            string userId)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);

                await conn.OpenAsync();

                if (translation.Id == 0)
                {
                    const string insertQuery = @"
                        INSERT INTO tbl_consent_Translations
                        (
                            ConsentId,
                            TenantId,
                            ProgramId,
                            LanguageCode,
                            [Text],
                            [Description]
                        )
                        VALUES
                        (
                            @ConsentId,
                            @TenantId,
                            @ProgramId,
                            @LanguageCode,
                            @Text,
                            @Description
                        );";

                    using var cmd = new SqlCommand(insertQuery, conn);

                    cmd.Parameters.AddWithValue("@ConsentId", translation.ConsentId);

                    cmd.Parameters.AddWithValue("@TenantId", translation.TenantId);

                    cmd.Parameters.AddWithValue("@ProgramId", translation.ProgramId);

                    cmd.Parameters.AddWithValue(
                        "@LanguageCode",
                        (object?)translation.LanguageCode ?? DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "@Text",
                        (object?)translation.Text ?? DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "@Description",
                        (object?)translation.Description ?? DBNull.Value);

                    await cmd.ExecuteNonQueryAsync();

                    _logger.LogInformation(
                        "Consent translation created for ConsentId {ConsentId} Language {LanguageCode}",
                        translation.ConsentId,
                        translation.LanguageCode);
                }
                else
                {
                    const string updateQuery = @"
                        UPDATE tbl_consent_Translations
                        SET
                            [Text] = @Text,
                            [Description] = @Description
                        WHERE ConsentId = @ConsentId
                          AND LanguageCode = @LanguageCode;";

                    using var cmd = new SqlCommand(updateQuery, conn);

                    cmd.Parameters.AddWithValue("@ConsentId", translation.ConsentId);

                    cmd.Parameters.AddWithValue(
                        "@LanguageCode",
                        (object?)translation.LanguageCode ?? DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "@Text",
                        (object?)translation.Text ?? DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "@Description",
                        (object?)translation.Description ?? DBNull.Value);

                    await cmd.ExecuteNonQueryAsync();

                    _logger.LogInformation(
                        "Consent translation updated for ConsentId {ConsentId} Language {LanguageCode}",
                        translation.ConsentId,
                        translation.LanguageCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error adding/updating translation for ConsentId {ConsentId}",
                    translation.ConsentId);

                throw;
            }
        }

        public async Task<IEnumerable<ConsentTranslationDto>> GetAllTranslationsAsync(
     int programId,
     int consentId)
        {
            try
            {
                const string query = @"
            SELECT
                Id,
                TenantId,
                ProgramId,
                ConsentId,
                LanguageCode,
                [Text],
                [Description]
            FROM tbl_consent_Translations
            WHERE ConsentId = @ConsentId
              AND ProgramId = @ProgramId;";

                var translations = new List<ConsentTranslationDto>();

                await using var conn = new SqlConnection(connectionString);
                await using var cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@ConsentId", consentId);
                cmd.Parameters.AddWithValue("@ProgramId", programId);

                _logger.LogInformation(
                    "Retrieving consent translations for ProgramId={ProgramId}, ConsentId={ConsentId}",
                    programId,
                    consentId);

                await conn.OpenAsync();

                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    translations.Add(new ConsentTranslationDto
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        TenantId = reader.GetInt32(reader.GetOrdinal("TenantId")),
                        ProgramId = reader.GetInt32(reader.GetOrdinal("ProgramId")),
                        ConsentId = reader.GetInt32(reader.GetOrdinal("ConsentId")),
                        LanguageCode = reader["LanguageCode"] as string,
                        Text = reader["Text"] as string,
                        Description = reader["Description"] as string
                    });
                }

                _logger.LogInformation(
                    "Retrieved {Count} translations for ProgramId={ProgramId}, ConsentId={ConsentId}",
                    translations.Count,
                    programId,
                    consentId);

                return translations;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving translations for ProgramId={ProgramId}, ConsentId={ConsentId}",
                    programId,
                    consentId);

                throw;
            }
        }

        public async Task DeleteTranslationAsync(int programId, int consentId, string languageCode)
        {
            const string query = @"
        DELETE FROM tbl_consent_Translations
        WHERE ConsentId = @ConsentId
          AND ProgramId = @ProgramId
          AND LanguageCode = @LanguageCode;";

            try
            {
                await using var conn = new SqlConnection(connectionString);
                await using var cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@ConsentId", consentId);
                cmd.Parameters.AddWithValue("@ProgramId", programId);
                cmd.Parameters.AddWithValue("@LanguageCode", languageCode);

                await conn.OpenAsync();

                var rows = await cmd.ExecuteNonQueryAsync();

                _logger.LogInformation(
                    "Deleted {Rows} translation(s) for ProgramId={ProgramId}, ConsentId={ConsentId}, LanguageCode={LanguageCode}",
                    rows,
                    programId,
                    consentId,
                    languageCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error deleting translation for ProgramId={ProgramId}, ConsentId={ConsentId}, LanguageCode={LanguageCode}",
                    programId,
                    consentId,
                    languageCode);

                throw;
            }
        }
    }
}