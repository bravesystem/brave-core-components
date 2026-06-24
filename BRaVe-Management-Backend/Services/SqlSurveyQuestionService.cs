using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Exceptions;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BRaVe_Management_Backend.Services
{
    public class SqlSurveyQuestionService : ISurveyQuestionService
    {
        private readonly string connectionString;
        private readonly ILogger<SqlSurveyQuestionService> _logger;

        public SqlSurveyQuestionService(ISecretProvider secretProvider, ILogger<SqlSurveyQuestionService> logger)
        {
            _logger = logger;
            connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection).Result;
        }

        // ---- GET ALL SURVEY QUESTIONS ----
        public async Task<IEnumerable<SurveyQuestionDto>> GetAllQuestionsBySurveyId(int SurveyId)
        {
            _logger.LogInformation("Fetching all survey questions for SurveyId={SurveyId}", SurveyId);

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();
                _logger.LogDebug("SQL connection opened successfully for GetAllQuestionsBySurveyId");

                var sql = @"SELECT SQ.*, T.Text FROM tbl_Surveys s 
                            INNER JOIN tbl_SurveyQuestions SQ ON s.SurveyCode=SQ.SurveyCode AND SQ.TenantId=s.TenantId
                            INNER JOIN tbl_SurveyQuestion_Translations T ON SQ.SurveyCode=T.SurveyCode AND SQ.Id=T.Id 
                            WHERE s.SurveyId=@SurveyId AND SQ.DefaultLang=T.LanguageCode";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@SurveyId", SurveyId);

                var questions = new List<SurveyQuestionDto>();

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var question = new SurveyQuestionDto
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        SurveyCode = reader.GetString(reader.GetOrdinal("SurveyCode")),
                        TenantId = reader.GetInt32(reader.GetOrdinal("TenantId")),
                        QuestionOrder = reader.GetInt32(reader.GetOrdinal("QuestionOrder")),
                        DefaultLang = reader.GetString(reader.GetOrdinal("DefaultLang")),
                        IsRequired = reader.GetBoolean(reader.GetOrdinal("IsRequired")),
                        AnswerType = reader.GetInt32(reader.GetOrdinal("AnswerType")),
                        LookupId = reader.IsDBNull(reader.GetOrdinal("LookupId")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("LookupId")),
                        DatasetId = reader.IsDBNull(reader.GetOrdinal("DatasetId")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("DatasetId")),
                        MinSelection = reader.IsDBNull(reader.GetOrdinal("MinSelection")) ? (short?)null : reader.GetInt16(reader.GetOrdinal("MinSelection")),
                        MaxSelection = reader.IsDBNull(reader.GetOrdinal("MaxSelection")) ? (short?)null : reader.GetInt16(reader.GetOrdinal("MaxSelection")),
                        Restriction = reader.IsDBNull(reader.GetOrdinal("Restriction")) ? null : reader.GetString(reader.GetOrdinal("Restriction")),
                        SkipLogic = reader.IsDBNull(reader.GetOrdinal("SkipLogic")) ? string.Empty : reader.GetString(reader.GetOrdinal("SkipLogic")),
                        ResultExpression = reader.IsDBNull(reader.GetOrdinal("ResultExpression")) ? string.Empty : reader.GetString(reader.GetOrdinal("ResultExpression")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                        QuestionText = reader.GetString(reader.GetOrdinal("Text")),
                    };
                    questions.Add(question);
                }

                _logger.LogInformation("Fetched {Count} survey questions for SurveyId={SurveyId}", questions.Count, SurveyId);
                return questions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all survey questions for SurveyId={SurveyId}", SurveyId);
                throw;
            }
        }

        // ---- GET ALL SURVEY QUESTIONS ----
        public async Task<IEnumerable<SurveyQuestionDto>> GetAllQuestionsBySurveyCode(string SurveyCode)
        {
            _logger.LogInformation("Fetching all survey questions for SurveyCode={SurveyCode}", SurveyCode);

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();
                _logger.LogDebug("SQL connection opened successfully for GetAllQuestionsBySurveyId");

                var sql = @"SELECT * FROM tbl_SurveyQuestions SQ 
                            INNER JOIN tbl_SurveyQuestion_Translations T 
                            ON SQ.SurveyCode=T.SurveyCode AND SQ.Id=T.Id 
                            WHERE SQ.SurveyCode=@SurveyCode AND SQ.DefaultLang=T.LanguageCode";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@SurveyCode", SurveyCode);

                var questions = new List<SurveyQuestionDto>();

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var question = new SurveyQuestionDto
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        SurveyCode = reader.GetString(reader.GetOrdinal("SurveyCode")),
                        TenantId = reader.GetInt32(reader.GetOrdinal("TenantId")),
                        QuestionOrder = reader.GetInt32(reader.GetOrdinal("QuestionOrder")),
                        DefaultLang = reader.GetString(reader.GetOrdinal("DefaultLang")),
                        IsRequired = reader.GetBoolean(reader.GetOrdinal("IsRequired")),
                        AnswerType = reader.GetInt32(reader.GetOrdinal("AnswerType")),
                        LookupId = reader.IsDBNull(reader.GetOrdinal("LookupId")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("LookupId")),
                        DatasetId = reader.IsDBNull(reader.GetOrdinal("DatasetId")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("DatasetId")),
                        MinSelection = reader.IsDBNull(reader.GetOrdinal("MinSelection")) ? (short?)null : reader.GetInt16(reader.GetOrdinal("MinSelection")),
                        MaxSelection = reader.IsDBNull(reader.GetOrdinal("MaxSelection")) ? (short?)null : reader.GetInt16(reader.GetOrdinal("MaxSelection")),
                        Restriction = reader.IsDBNull(reader.GetOrdinal("Restriction")) ? null : reader.GetString(reader.GetOrdinal("Restriction")),
                        SkipLogic = reader.IsDBNull(reader.GetOrdinal("SkipLogic")) ? string.Empty : reader.GetString(reader.GetOrdinal("SkipLogic")),
                        ResultExpression = reader.IsDBNull(reader.GetOrdinal("ResultExpression")) ? string.Empty : reader.GetString(reader.GetOrdinal("ResultExpression")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                        QuestionText = reader.GetString(reader.GetOrdinal("Text")),
                    };
                    questions.Add(question);
                }

                _logger.LogInformation("Fetched {Count} survey questions for SurveyCode={SurveyCode}", questions.Count, SurveyCode);
                return questions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all survey questions for SurveyCode={SurveyCode}", SurveyCode);
                throw;
            }
        }

        // ---- GET SPECIFIC QUESTION ----
        public async Task<SurveyQuestionDto?> GetQuestionById(string SurveyCode, int questionId)
        {
            _logger.LogInformation("Fetching question by ID: SurveyCode={SurveyCode}, QuestionId={QuestionId}", SurveyCode, questionId);

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = "SELECT TOP 1 * FROM tbl_SurveyQuestions WHERE SurveyCode=@SurveyCode AND Id=@Id";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@SurveyCode", SurveyCode);
                cmd.Parameters.AddWithValue("@Id", questionId);

                using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    _logger.LogInformation("Question found for SurveyCode={SurveyCode}, QuestionId={QuestionId}", SurveyCode, questionId);
                    return new SurveyQuestionDto
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        SurveyCode = reader.GetString(reader.GetOrdinal("SurveyCode")),
                        TenantId = reader.GetInt32(reader.GetOrdinal("TenantId")),
                        QuestionOrder = reader.GetInt32(reader.GetOrdinal("QuestionOrder")),
                        DefaultLang = reader.IsDBNull(reader.GetOrdinal("DefaultLang")) ? null : reader.GetString(reader.GetOrdinal("DefaultLang")),
                        IsRequired = reader.GetBoolean(reader.GetOrdinal("IsRequired")),
                        AnswerType = reader.GetInt32(reader.GetOrdinal("AnswerType")),
                        LookupId = reader.IsDBNull(reader.GetOrdinal("LookupId")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("LookupId")),
                        DatasetId = reader.IsDBNull(reader.GetOrdinal("DatasetId")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("DatasetId")),
                        MinSelection = reader.IsDBNull(reader.GetOrdinal("MinSelection")) ? (short?)null : reader.GetInt16(reader.GetOrdinal("MinSelection")),
                        MaxSelection = reader.IsDBNull(reader.GetOrdinal("MaxSelection")) ? (short?)null : reader.GetInt16(reader.GetOrdinal("MaxSelection")),
                        Restriction = reader.IsDBNull(reader.GetOrdinal("Restriction")) ? null : reader.GetString(reader.GetOrdinal("Restriction")),
                        SkipLogic = reader.IsDBNull(reader.GetOrdinal("SkipLogic")) ? string.Empty : reader.GetString(reader.GetOrdinal("SkipLogic")),
                        ResultExpression = reader.IsDBNull(reader.GetOrdinal("ResultExpression")) ? string.Empty : reader.GetString(reader.GetOrdinal("ResultExpression")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                    };
                }

                _logger.LogWarning("No question found for SurveyCode={SurveyCode}, QuestionId={QuestionId}", SurveyCode, questionId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching question: SurveyCode={SurveyCode}, QuestionId={QuestionId}", SurveyCode, questionId);
                throw new Exception("Error fetching question", ex);
            }
        }

        // ---- CREATE QUESTION ----
        public async Task CreateSurveyQuestion(string userId, SurveyQuestionDto data)
        {
            _logger.LogInformation("Creating new survey question: SurveyCode={SurveyCode}, CreatedBy={UserId}", data.SurveyCode, userId);

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("sp_CreateSurveyQuestion", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@SurveyCode", data.SurveyCode);
                cmd.Parameters.AddWithValue("@TenantId", data.TenantId);
                cmd.Parameters.AddWithValue("@QuestionOrder", data.QuestionOrder);
                cmd.Parameters.AddWithValue("@DefaultLang", data.DefaultLang ?? "en");
                cmd.Parameters.AddWithValue("@IsRequired", data.IsRequired);
                cmd.Parameters.AddWithValue("@AnswerType", data.AnswerType);
                cmd.Parameters.AddWithValue("@LookupId", data.LookupId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@DatasetId", data.DatasetId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@MinSelection", data.MinSelection ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@MaxSelection", data.MaxSelection ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Restriction", data.Restriction ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@SkipLogic", data.SkipLogic ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ResultExpression", data.ResultExpression ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@IsActive", data.IsActive);
                cmd.Parameters.AddWithValue("@CreatedByUserId", userId);
                cmd.Parameters.AddWithValue("@QuestionText", data.QuestionText);

                await cmd.ExecuteNonQueryAsync();

                _logger.LogInformation("Survey question created successfully for SurveyCode={SurveyCode}", data.SurveyCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating survey question for SurveyCode={SurveyCode}", data.SurveyCode);
                throw new Exception($"Error creating survey question: {ex.Message}", ex);
            }
        }

        // ---- UPDATE QUESTION ----
        public async Task UpdateSurveyQuestion(string userId, SurveyQuestionDto data, int id, string surveyCode)
        {
            _logger.LogInformation("Updating question: SurveyCode={SurveyCode}, QuestionId={QuestionId}, UpdatedBy={UserId}", surveyCode, id, userId);

            const string query = @"
            BEGIN TRANSACTION;
            UPDATE [dbo].[tbl_SurveyQuestions]
            SET TenantId=@TenantId, QuestionOrder=@QuestionOrder, DefaultLang=@DefaultLang,
                IsRequired=@IsRequired, AnswerType=@AnswerType, LookupId=@LookupId,
                DatasetId=@DatasetId, MinSelection=@MinSelection, MaxSelection=@MaxSelection,
                Restriction=@Restriction, SkipLogic=@SkipLogic, ResultExpression=@ResultExpression,
                IsActive=@IsActive, UpdatedByUserId=@UpdatedByUserId, UpdatedOn=GETUTCDATE()
            WHERE SurveyCode=@SurveyCode AND Id=@Id;

            UPDATE [dbo].[tbl_SurveyQuestion_Translations]
            SET [Text]=@QuestionText
            WHERE SurveyCode=@SurveyCode AND Id=@Id AND LanguageCode=@DefaultLang;
            COMMIT TRANSACTION;";

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@SurveyCode", surveyCode);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@TenantId", data.TenantId);
                cmd.Parameters.AddWithValue("@QuestionOrder", data.QuestionOrder);
                cmd.Parameters.AddWithValue("@DefaultLang", data.DefaultLang ?? "en");
                cmd.Parameters.AddWithValue("@IsRequired", data.IsRequired);
                cmd.Parameters.AddWithValue("@AnswerType", data.AnswerType);
                cmd.Parameters.AddWithValue("@LookupId", data.LookupId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@DatasetId", data.DatasetId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@MinSelection", data.MinSelection ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@MaxSelection", data.MaxSelection ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Restriction", data.Restriction ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@SkipLogic", data.SkipLogic ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ResultExpression", data.ResultExpression ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@IsActive", data.IsActive);
                cmd.Parameters.AddWithValue("@UpdatedByUserId", userId);
                cmd.Parameters.AddWithValue("@QuestionText", data.QuestionText ?? (object)DBNull.Value);

                var rows = await cmd.ExecuteNonQueryAsync();

                if (rows == 0)
                {
                    _logger.LogWarning("No survey question found for update: SurveyCode={SurveyCode}, QuestionId={QuestionId}", surveyCode, id);
                    throw new InvalidOperationException($"No survey question found for SurveyCode '{surveyCode}' and Id {id}.");
                }

                _logger.LogInformation("Survey question updated successfully: SurveyCode={SurveyCode}, QuestionId={QuestionId}", surveyCode, id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating survey question: SurveyCode={SurveyCode}, QuestionId={QuestionId}", surveyCode, id);
                throw new Exception($"Error updating survey question: {ex.Message}", ex);
            }
        }

        // ---- DELETE QUESTION ----
        public async Task DeleteSurveyQuestion(string userId, int TenantId, int id, string SurveyCode)
        {
            _logger.LogInformation("Deleting question: SurveyCode={SurveyCode}, QuestionId={QuestionId}, DeletedBy={UserId}", SurveyCode, id, userId);

            try
            {
                using var conn = new SqlConnection(connectionString);

                await conn.OpenAsync();

                using (var cmd = new SqlCommand("sp_DeleteSurveyQuestion", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@SurveyCode", SurveyCode);
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@TenantId", TenantId);
                    await cmd.ExecuteNonQueryAsync();
                }

                _logger.LogInformation("Survey question deleted successfully: SurveyCode={SurveyCode}, QuestionId={QuestionId}", SurveyCode, id);
            }
            catch (SqlException ex) when (ex.Number == 50010)
            {
                _logger.LogError(ex, "Error deleting survey question: SurveyCode={SurveyCode}, QuestionId={QuestionId}", SurveyCode, id);
                throw;
            }
            catch (SqlException ex) when (ex.Number == 50020)
            {
                //Deletion not allowed. The survey question has already been downloaded to a field device for an activity and has been deactivated instead.
                _logger.LogError(ex, "Hard delete is not allowed. The survey question has been soft deleted instead. SurveyCode={SurveyCode}, QuestionId={QuestionId}", SurveyCode, id);
                throw;
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error deleting survey question: SurveyCode={SurveyCode}, QuestionId={QuestionId}", SurveyCode, id);
                throw new Exception($"Error deleting survey question: {ex.Message}", ex);
            }
        }

        // ---- GET TRANSLATIONS ----
        public async Task<List<SurveyQuestionTranslationDto>> GetAllSurveyQuestionTranslations(string surveyCode, int questionId, int tenantId)
        {
            _logger.LogInformation("Fetching all translations: SurveyCode={SurveyCode}, QuestionId={QuestionId}, TenantId={TenantId}", surveyCode, questionId, tenantId);

            const string query = @"SELECT SurveyCode, Id, TenantId, LanguageCode, [Text]
                                   FROM [dbo].[tbl_SurveyQuestion_Translations]
                                   WHERE SurveyCode=@SurveyCode AND Id=@Id AND TenantId=@TenantId
                                   ORDER BY LanguageCode;";

            var translations = new List<SurveyQuestionTranslationDto>();

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@SurveyCode", surveyCode);
                cmd.Parameters.AddWithValue("@Id", questionId);
                cmd.Parameters.AddWithValue("@TenantId", tenantId);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    translations.Add(new SurveyQuestionTranslationDto
                    {
                        SurveyCode = reader["SurveyCode"].ToString(),
                        Id = Convert.ToInt32(reader["Id"]),
                        TenantId = Convert.ToInt32(reader["TenantId"]),
                        LanguageCode = reader["LanguageCode"].ToString(),
                        Text = reader["Text"].ToString()
                    });
                }

                _logger.LogInformation("Fetched {Count} translations for QuestionId={QuestionId}", translations.Count, questionId);
                return translations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching translations: SurveyCode={SurveyCode}, QuestionId={QuestionId}", surveyCode, questionId);
                throw;
            }
        }

        // ---- CREATE TRANSLATION ----
        public async Task CreateSurveyQuestionTranslation(SurveyQuestionTranslationDto data)
        {
            _logger.LogInformation("Creating translation: SurveyCode={SurveyCode}, QuestionId={QuestionId}, LanguageCode={LanguageCode}", data.SurveyCode, data.Id, data.LanguageCode);

            const string query = @"INSERT INTO [dbo].[tbl_SurveyQuestion_Translations]
                                   (SurveyCode, Id, TenantId, LanguageCode, [Text])
                                   VALUES (@SurveyCode, @Id, @TenantId, @LanguageCode, @Text);";

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@SurveyCode", data.SurveyCode);
                cmd.Parameters.AddWithValue("@Id", data.Id);
                cmd.Parameters.AddWithValue("@TenantId", data.TenantId);
                cmd.Parameters.AddWithValue("@LanguageCode", data.LanguageCode);
                cmd.Parameters.AddWithValue("@Text", data.Text);

                await cmd.ExecuteNonQueryAsync();
                _logger.LogInformation("Translation created successfully for QuestionId={QuestionId}, LanguageCode={LanguageCode}", data.Id, data.LanguageCode);
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2601 || ex.Number == 2627)
                {
                    _logger.LogWarning("Duplicate translation detected: SurveyCode={SurveyCode}, QuestionId={QuestionId}, LanguageCode={LanguageCode}", data.SurveyCode, data.Id, data.LanguageCode);
                    throw new InvalidOperationException("A translation for this language already exists for this question.");
                }
                _logger.LogError(ex, "SQL error creating translation: SurveyCode={SurveyCode}, QuestionId={QuestionId}", data.SurveyCode, data.Id);
                throw;
            }
        }


        // ---- UPDATE SURVEY QUESTION TRANSLATION ----
        public async Task UpdateSurveyQuestionTranslation(SurveyQuestionTranslationDto data)
        {
            _logger.LogInformation(
                "Updating survey question translation: SurveyCode={SurveyCode}, QuestionId={QuestionId}, LanguageCode={LanguageCode}, TenantId={TenantId}",
                data.SurveyCode, data.Id, data.LanguageCode, data.TenantId);

            const string query = @"
        UPDATE [dbo].[tbl_SurveyQuestion_Translations]
        SET [Text] = @Text
        WHERE SurveyCode = @SurveyCode
          AND Id = @Id
          AND LanguageCode = @LanguageCode
          AND TenantId = @TenantId;";

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();
                _logger.LogDebug("SQL connection opened for UpdateSurveyQuestionTranslation (SurveyCode={SurveyCode}, QuestionId={QuestionId})",
                    data.SurveyCode, data.Id);

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@SurveyCode", data.SurveyCode);
                cmd.Parameters.AddWithValue("@Id", data.Id);
                cmd.Parameters.AddWithValue("@TenantId", data.TenantId);
                cmd.Parameters.AddWithValue("@LanguageCode", data.LanguageCode);
                cmd.Parameters.AddWithValue("@Text", data.Text);

                var rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected == 0)
                {
                    _logger.LogWarning("No translation found for update: SurveyCode={SurveyCode}, QuestionId={QuestionId}, LanguageCode={LanguageCode}",
                        data.SurveyCode, data.Id, data.LanguageCode);
                    throw new KeyNotFoundException("Translation not found for update.");
                }

                _logger.LogInformation("Translation updated successfully: SurveyCode={SurveyCode}, QuestionId={QuestionId}, LanguageCode={LanguageCode}",
                    data.SurveyCode, data.Id, data.LanguageCode);
            }
            catch (KeyNotFoundException)
            {
                throw; // preserve behavior
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating translation: SurveyCode={SurveyCode}, QuestionId={QuestionId}, LanguageCode={LanguageCode}",
                    data.SurveyCode, data.Id, data.LanguageCode);
                throw;
            }
        }


        // ---- DELETE SURVEY QUESTION TRANSLATION ----
        public async Task DeleteSurveyQuestionTranslation(SurveyQuestionTranslationDto data)
        {
            _logger.LogInformation(
                "Deleting survey question translation: SurveyCode={SurveyCode}, QuestionId={QuestionId}, LanguageCode={LanguageCode}, TenantId={TenantId}",
                data.SurveyCode, data.Id, data.LanguageCode, data.TenantId);

            const string query = @"
        DELETE FROM [dbo].[tbl_SurveyQuestion_Translations]
        WHERE SurveyCode = @SurveyCode
          AND Id = @Id
          AND LanguageCode = @LanguageCode
          AND TenantId = @TenantId;";

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();
                _logger.LogDebug("SQL connection opened for DeleteSurveyQuestionTranslation (SurveyCode={SurveyCode}, QuestionId={QuestionId})",
                    data.SurveyCode, data.Id);

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@SurveyCode", data.SurveyCode);
                cmd.Parameters.AddWithValue("@Id", data.Id);
                cmd.Parameters.AddWithValue("@TenantId", data.TenantId);
                cmd.Parameters.AddWithValue("@LanguageCode", data.LanguageCode);

                var rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected == 0)
                {
                    _logger.LogWarning("No translation found or already deleted: SurveyCode={SurveyCode}, QuestionId={QuestionId}, LanguageCode={LanguageCode}",
                        data.SurveyCode, data.Id, data.LanguageCode);
                    throw new KeyNotFoundException("Translation not found or already deleted.");
                }

                _logger.LogInformation("Translation deleted successfully: SurveyCode={SurveyCode}, QuestionId={QuestionId}, LanguageCode={LanguageCode}",
                    data.SurveyCode, data.Id, data.LanguageCode);
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting translation: SurveyCode={SurveyCode}, QuestionId={QuestionId}, LanguageCode={LanguageCode}",
                    data.SurveyCode, data.Id, data.LanguageCode);
                throw;
            }
        }



    }
}


