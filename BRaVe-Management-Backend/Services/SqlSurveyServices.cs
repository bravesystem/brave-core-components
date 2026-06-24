using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Exceptions;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using DocumentFormat.OpenXml.Drawing.Charts;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BRaVe_Management_Backend.Services
{
    public class SqlSurveyServices : ISurveyService
    {
        private readonly string connectionString;
        private readonly ILogger<SqlSurveyServices> _logger;

        public SqlSurveyServices(ISecretProvider secretProvider, ILogger<SqlSurveyServices> logger)
        {
            _logger = logger;
            connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection).Result;
        }


        public async Task<IEnumerable<Surveys>> GetAllSurveys(string userId, int tenantId)
        {
            _logger.LogInformation("Fetching all surveys for user {userId}", userId);

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("dbo.sp_GetAllSurveys", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@TenantId", tenantId);

                var surveys = new List<Surveys>();

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    surveys.Add(new Surveys
                    {
                        SurveyId = reader.GetInt32(reader.GetOrdinal("SurveyId")),
                        ProgramId = reader.GetInt32(reader.GetOrdinal("ProgramId")),
                        ProgramName = reader.GetString(reader.GetOrdinal("ProgramTitle")),
                        SurveyCode = reader.GetString(reader.GetOrdinal("SurveyCode")),
                        SurveyType = reader.GetInt32(reader.GetOrdinal("SurveyType")),
                        TenantId = reader.IsDBNull(reader.GetOrdinal("TenantId")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("TenantId")),
                        Title = reader.IsDBNull(reader.GetOrdinal("Title")) ? null : reader.GetString(reader.GetOrdinal("Title")),
                        Details = reader.IsDBNull(reader.GetOrdinal("Details")) ? string.Empty : reader.GetString(reader.GetOrdinal("Details")),
                        IsActive = reader.IsDBNull(reader.GetOrdinal("IsActive")) ? (bool?)null : reader.GetBoolean(reader.GetOrdinal("IsActive")),
                        CreatedByUserId = reader.IsDBNull(reader.GetOrdinal("CreatedByUserId")) ? null : reader.GetString(reader.GetOrdinal("CreatedByUserId")),
                        UpdatedByUserId = reader.IsDBNull(reader.GetOrdinal("UpdatedByUserId")) ? null : reader.GetString(reader.GetOrdinal("UpdatedByUserId")),
                        CreatedOn = reader.GetDateTime(reader.GetOrdinal("CreatedOn")),
                        UpdatedOn = reader.GetDateTime(reader.GetOrdinal("UpdatedOn")),

                        TotalQuestions = reader.IsDBNull(reader.GetOrdinal("TotalQuestions")) ? 0 : reader.GetInt32(reader.GetOrdinal("TotalQuestions")),
                    });
                }

                _logger.LogInformation("Fetched {Count} surveys for user {userId}", surveys.Count, userId);
                return surveys;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching surveys for user {userId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<Surveys>> GetAllSurveyByProgramId(int TenantId,int ProgramId)
        {
            _logger.LogInformation("Fetching all surveys for ProgramId {ProgramId}", ProgramId);

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                //var sql = "SELECT P.Title AS ProgramTitle, s.* FROM tbl_Surveys S inner join tbl_RegistrationPrograms p on p.programid=s.programid  WHERE S.ProgramId=@ProgramId AND s.Deleted=0";
                using var cmd = new SqlCommand("sp_GetAllSurveysByProgramId", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@TenantId", TenantId);
                cmd.Parameters.AddWithValue("@ProgramId", ProgramId);

                var surveys = new List<Surveys>();

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    surveys.Add(new Surveys
                    {
                        SurveyId = reader.GetInt32(reader.GetOrdinal("SurveyId")),
                        ProgramId = reader.GetInt32(reader.GetOrdinal("ProgramId")),
                        ProgramName = reader.GetString(reader.GetOrdinal("ProgramTitle")),
                        SurveyCode = reader.GetString(reader.GetOrdinal("SurveyCode")),
                        SurveyType = reader.GetInt32(reader.GetOrdinal("SurveyType")),
                        TenantId = reader.IsDBNull(reader.GetOrdinal("TenantId")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("TenantId")),
                        Title = reader.IsDBNull(reader.GetOrdinal("Title")) ? null : reader.GetString(reader.GetOrdinal("Title")),
                        Details = reader.IsDBNull(reader.GetOrdinal("Details")) ? string.Empty : reader.GetString(reader.GetOrdinal("Details")),
                        IsActive = reader.IsDBNull(reader.GetOrdinal("IsActive")) ? (bool?)null : reader.GetBoolean(reader.GetOrdinal("IsActive")),
                        CreatedByUserId = reader.IsDBNull(reader.GetOrdinal("CreatedByUserId")) ? null : reader.GetString(reader.GetOrdinal("CreatedByUserId")),
                        UpdatedByUserId = reader.IsDBNull(reader.GetOrdinal("UpdatedByUserId")) ? null : reader.GetString(reader.GetOrdinal("UpdatedByUserId")),
                        CreatedOn = reader.GetDateTime(reader.GetOrdinal("CreatedOn")),
                        UpdatedOn = reader.GetDateTime(reader.GetOrdinal("UpdatedOn")),

                        TotalQuestions = reader.IsDBNull(reader.GetOrdinal("TotalQuestions")) ? 0 : reader.GetInt32(reader.GetOrdinal("TotalQuestions"))

                    });
                }

                _logger.LogInformation("Fetched {Count} surveys for ProgramId {ProgramId}", surveys.Count, ProgramId);
                return surveys;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching surveys for ProgramId {ProgramId}", ProgramId);
                throw;
            }
        }
        // GET SURVEY BY SURVEY ID
        public async Task<Surveys?> GetSurveyById(int surveyId)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = "SELECT * FROM tbl_Surveys WHERE SurveyId=@SurveyId";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@SurveyId", surveyId);

                using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var survey = new Surveys()
                    {
                        SurveyId = reader.GetInt32(reader.GetOrdinal("SurveyId")),
                        ProgramId = reader.GetInt32(reader.GetOrdinal("ProgramId")),
                        SurveyCode = reader.GetString(reader.GetOrdinal("SurveyCode")),
                        SurveyType = reader.GetInt32(reader.GetOrdinal("SurveyType")),
                        TenantId = reader.IsDBNull(reader.GetOrdinal("TenantId")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("TenantId")),
                        Title = reader.IsDBNull(reader.GetOrdinal("Title")) ? null : reader.GetString(reader.GetOrdinal("Title")),
                        Details = reader.IsDBNull(reader.GetOrdinal("Details")) ? string.Empty : reader.GetString(reader.GetOrdinal("Details")),
                        IsActive = reader.IsDBNull(reader.GetOrdinal("IsActive")) ? (bool?)null : reader.GetBoolean(reader.GetOrdinal("IsActive")),
                        CreatedByUserId = reader.IsDBNull(reader.GetOrdinal("CreatedByUserId")) ? null : reader.GetString(reader.GetOrdinal("CreatedByUserId")),
                        UpdatedByUserId = reader.IsDBNull(reader.GetOrdinal("UpdatedByUserId")) ? null : reader.GetString(reader.GetOrdinal("UpdatedByUserId")),
                        CreatedOn = reader.GetDateTime(reader.GetOrdinal("CreatedOn")),
                        UpdatedOn = reader.GetDateTime(reader.GetOrdinal("UpdatedOn"))
                    };

                    return survey;
                }

                return null;
            }
            catch (Exception)
            {
                throw;
            }
        }


       public async Task CreateSurvey(string UserId , SurveyDto data)
        {
            _logger.LogInformation("Creating survey for ProgramId {ProgramId} by User {UserId}", data.ProgramId, UserId);

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("sp_CreateSurvey", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@SurveyType", data.SurveyType);
                cmd.Parameters.AddWithValue("@TenantId", data.TenantId);
                cmd.Parameters.AddWithValue("@Title", data.Title);
                cmd.Parameters.AddWithValue("@Details", data.Details ?? string.Empty);
                cmd.Parameters.AddWithValue("@IsActive", data.IsActive);
                cmd.Parameters.AddWithValue("@ProgramId", data.ProgramId);
                cmd.Parameters.AddWithValue("@CreatedByUserId", UserId);

                await cmd.ExecuteNonQueryAsync();
                _logger.LogInformation("Survey created successfully for ProgramId {ProgramId}", data.ProgramId);
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Error creating survey for ProgramId {ProgramId} by User {UserId}", data.ProgramId, UserId);
                throw;
            }
            
       }

        public async Task UpdateSurvey(string UserId, SurveyDto data)
        {
            _logger.LogInformation("Updating survey {SurveyId} by User {UserId}", data.SurveyId, UserId);

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("sp_UpdateSurvey", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@SurveyId", data.SurveyId);
                cmd.Parameters.AddWithValue("@SurveyType", data.SurveyType);
                cmd.Parameters.AddWithValue("@TenantId", data.TenantId);
                cmd.Parameters.AddWithValue("@Title", data.Title);
                cmd.Parameters.AddWithValue("@Details", data.Details ?? string.Empty);
                cmd.Parameters.AddWithValue("@IsActive", data.IsActive);
                cmd.Parameters.AddWithValue("@UpdatedByUserId", UserId);

                await cmd.ExecuteNonQueryAsync();
                _logger.LogInformation("Survey {SurveyId} updated successfully by User {UserId}", data.SurveyId, UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating survey {SurveyId} by User {UserId}", data.SurveyId, UserId);
                throw;
            }
        }

        //ACTIVATE SURVEY
        public async Task ActivateSurvey(string UserId, int SurveyId)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                string query = "UPDATE tbl_Surveys SET IsActive=1,UpdatedByUserId=@UpdatedByUserId,UpdatedOn=GETUTCDATE() WHERE SurveyId = @SurveyId";

                using var cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@SurveyId",SurveyId);
                cmd.Parameters.AddWithValue("@UpdatedByUserId", UserId);

                await cmd.ExecuteNonQueryAsync();

            }
            catch (Exception e)
            {
                throw e;
            }

        }


        //DELETE SURVEY
        public async Task DeleteSurvey(string UserId, DeleteSurveyDto data)
        {
            _logger.LogInformation("Deleting survey {SurveyId} for Tenant {TenantId} by User {UserId}", data.SurveyId, data.TenantId, UserId);

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                string query = "UPDATE tbl_Surveys SET Deleted=1, UpdatedByUserId=@UpdatedByUserId, UpdatedOn=GETUTCDATE() WHERE SurveyId=@SurveyId AND TenantId=@TenantId";
                using var cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@SurveyId", data.SurveyId);
                cmd.Parameters.AddWithValue("@TenantId", data.TenantId);
                cmd.Parameters.AddWithValue("@UpdatedByUserId", UserId);

                await cmd.ExecuteNonQueryAsync();
                _logger.LogInformation("Survey {SurveyId} marked as deleted by User {UserId}", data.SurveyId, UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting survey {SurveyId} for Tenant {TenantId} by User {UserId}", data.SurveyId, data.TenantId, UserId);
                throw;
            }
        }
    }
}
