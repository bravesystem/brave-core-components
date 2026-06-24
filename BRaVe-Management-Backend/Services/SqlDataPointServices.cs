using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace BRaVe_Management_Backend.Services
{
    public class SqlDataPointServices : IDatapointService
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlDataPointServices> _logger;

        public SqlDataPointServices(ISecretProvider secretProvider, ILogger<SqlDataPointServices> logger)
        {
            _logger = logger;
            _connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection).Result;
        }

        public async Task CreateDataPoint(DataPointDto newDataPoint, string UserId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("sp_CreateDatapoint", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

              
                cmd.Parameters.AddWithValue("@Id", newDataPoint.Id);
                cmd.Parameters.AddWithValue("@Text", newDataPoint.Text);
                cmd.Parameters.AddWithValue("@DefaultLang", newDataPoint.DefaultLang ?? "en");
                cmd.Parameters.AddWithValue("@Description", newDataPoint.Description ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@TenantId", newDataPoint.TenantId);
                cmd.Parameters.AddWithValue("@CategoryId", newDataPoint.CategoryId);
                cmd.Parameters.AddWithValue("@AnswerType", newDataPoint.AnswerType);
                cmd.Parameters.AddWithValue("@LookupId", newDataPoint.LookupId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@DatasetId", newDataPoint.DatasetId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@MinSelection", newDataPoint.MinSelection ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@MaxSelection", newDataPoint.MaxSelection ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Restriction", newDataPoint.Restriction ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@CreatedByUserId",UserId);
                //cmd.Parameters.AddWithValue("@CreatedOn",DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@IsActive", newDataPoint.IsActive);

                newDataPoint.Id = (int)await cmd.ExecuteScalarAsync();
                _logger.LogInformation("Created datapoint {Id} for tenant {TenantId}", newDataPoint.Id, newDataPoint.TenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating datapoint for tenant {TenantId}", newDataPoint.TenantId);
                throw;
            }
        }

        public async Task<IEnumerable<DataPointDto>> GetAllDataPoints()
        {
            var dataPoints = new List<DataPointDto>();

            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var sql = "SELECT * FROM tbl_DataPoints";

                using var cmd = new SqlCommand(sql, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    dataPoints.Add(MapReaderToDataPoint(reader));
                }

                _logger.LogInformation("Fetched {Count} datapoints.", dataPoints.Count);
                return dataPoints;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching datapoints.");
                throw;
            }
        }
        public async Task DeleteDatapoint(string userId, int id, int TenatId)
        {
            _logger.LogInformation("Deleting Datapoint: TenatId={TenatId}, Id={Id}, DeletedBy={UserId}", TenatId, id, TenatId);

            const string deleteTranslationsQuery = @"DELETE FROM [dbo].[tbl_DataPoint_Translations] WHERE AttribubteId=@Id;";
            const string deleteQuestionQuery = @"DELETE FROM [dbo].[tbl_DataPoints] WHERE Id=@Id;";

            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                using (var cmd = new SqlCommand(deleteTranslationsQuery, conn))
                {
                    
                    cmd.Parameters.AddWithValue("@Id", id);
                    await cmd.ExecuteNonQueryAsync();
                }

                using (var cmd = new SqlCommand(deleteQuestionQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@TenatId", TenatId);
                    cmd.Parameters.AddWithValue("@Id", id);
                    int rows = await cmd.ExecuteNonQueryAsync();

                    if (rows == 0)
                    {
                        _logger.LogWarning("No datapoint found for deletion: datapoint={Id}, Tenat={TenatId}", TenatId, id);
                        
                    }
                }

                _logger.LogInformation("Datapoint deleted successfully: datapointId={Id}, ", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting datapoint question: datapoint id={id}, ",id);
                throw new Exception($"Error deleting survey question: {ex.Message}", ex);
            }
        }

        public async Task<DataPointDto> GetDataPointById(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var sql = @"
        SELECT d.*, t.Text
        FROM tbl_DataPoints d
        LEFT JOIN tbl_DataPoint_Translations t
            ON t.AttribubteId = d.Id
        WHERE d.Id = @Id";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Id", id);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapReaderToDataPoint(reader);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching datapoint {Id}", id);
                throw;
            }
        }




        public async Task<List<DataPointDto>> GetDataPointsForActivityByTenantAsync(int tenantId, string languageCode)
        {
            var results = new List<DataPointDto>();

            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var sql = @"SELECT d.*, t.Text
                    FROM tbl_DataPoints d
                    LEFT JOIN tbl_DataPoint_Translations t ON t.AttribubteId = d.Id
                    WHERE TenantId = @TenantId And t.LanguageCode= @LanguageCode AND IsActive=1
                     ORDER BY Id DESC;
                             ";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@TenantId", tenantId);
                cmd.Parameters.AddWithValue("@LanguageCode", languageCode);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(MapReaderToDataPoint(reader));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching data points for tenant {TenantId}", tenantId);
                throw;
            }

            return results;
        }


        public async Task<List<DataPointDto>> GetDataPointsByTenantAsync(int tenantId, string languageCode)
        {
            var results = new List<DataPointDto>();

            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var sql = @"SELECT d.*, t.Text
                    FROM tbl_DataPoints d
                    LEFT JOIN tbl_DataPoint_Translations t ON t.AttribubteId = d.Id
                    WHERE TenantId = @TenantId And t.LanguageCode= @LanguageCode
                     ORDER BY Id DESC;
                             ";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@TenantId", tenantId);
                cmd.Parameters.AddWithValue("@LanguageCode", languageCode);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(MapReaderToDataPoint(reader));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching data points for tenant {TenantId}", tenantId);
                throw;
            }

            return results;
        }

        public async Task UpdateDataPoint(DataPointDto updatedDataPoint)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var sql = @"
                BEGIN TRANSACTION;
                    UPDATE tbl_DataPoints
                    SET
                        CategoryId = @CategoryId,
                        AnswerType = @AnswerType,
                        LookupId = @LookupId,
                        DatasetId = @DatasetId,
                        MinSelection = @MinSelection,
                        MaxSelection = @MaxSelection,
                        Restriction = @Restriction,
                        IsActive = @IsActive,
                        UpdatedByUserId = @UpdatedByUserId,
                        UpdatedOn = @UpdatedOn
                    WHERE Id = @Id;

                UPDATE [dbo].[tbl_DataPoint_Translations]
                SET [Text]=@Text, description = @Description
                WHERE AttribubteId=@Id AND LanguageCode=@DefaultLang;
                COMMIT TRANSACTION;";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Id", updatedDataPoint.Id);
                cmd.Parameters.AddWithValue("@Description", updatedDataPoint.Description ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@CategoryId", updatedDataPoint.CategoryId);
                cmd.Parameters.AddWithValue("@AnswerType", updatedDataPoint.AnswerType);
                cmd.Parameters.AddWithValue("@LookupId", updatedDataPoint.LookupId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@DatasetId", updatedDataPoint.DatasetId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@MinSelection", updatedDataPoint.MinSelection ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@MaxSelection", updatedDataPoint.MaxSelection ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Restriction", updatedDataPoint.Restriction ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@UpdatedByUserId", updatedDataPoint.UpdatedByUserId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@UpdatedOn", updatedDataPoint.UpdatedOn ?? DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@Text", updatedDataPoint.Text);
                cmd.Parameters.AddWithValue("@DefaultLang", updatedDataPoint.DefaultLang ?? "en");
                cmd.Parameters.AddWithValue("@TenantId", updatedDataPoint.TenantId);
                cmd.Parameters.AddWithValue("@IsActive", updatedDataPoint.IsActive);

                await cmd.ExecuteNonQueryAsync();
                _logger.LogInformation("Updated datapoint {Id}", updatedDataPoint.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating datapoint {Id}", updatedDataPoint.Id);
                throw;
            }
        }
        private DataPointDto MapReaderToDataPoint(SqlDataReader reader)
        {
            return new DataPointDto
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Text = reader.IsDBNull(reader.GetOrdinal("Text"))
    ? null
    : reader.GetString(reader.GetOrdinal("Text")),

                TenantId = reader.GetInt32(reader.GetOrdinal("TenantId")),
                CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                AnswerType = reader.GetInt32(reader.GetOrdinal("AnswerType")),

                LookupId = reader.IsDBNull(reader.GetOrdinal("LookupId"))
                    ? (int?)null
                    : reader.GetInt32(reader.GetOrdinal("LookupId")),

                DatasetId = reader.IsDBNull(reader.GetOrdinal("DatasetId"))
                    ? (int?)null
                    : reader.GetInt32(reader.GetOrdinal("DatasetId")),

                MinSelection = reader.IsDBNull(reader.GetOrdinal("MinSelection"))
                    ? (short?)null
                    : reader.GetInt16(reader.GetOrdinal("MinSelection")),

                MaxSelection = reader.IsDBNull(reader.GetOrdinal("MaxSelection"))
                    ? (short?)null
                    : reader.GetInt16(reader.GetOrdinal("MaxSelection")),

                Restriction = reader.IsDBNull(reader.GetOrdinal("Restriction"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Restriction")),

                IsActive = reader.IsDBNull(reader.GetOrdinal("IsActive"))
            ? false
            : reader.GetBoolean(reader.GetOrdinal("IsActive")),

                CreatedByUserId =  reader.GetString(reader.GetOrdinal("CreatedByUserId")),

                UpdatedByUserId = reader.IsDBNull(reader.GetOrdinal("UpdatedByUserId"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("UpdatedByUserId")),

                CreatedOn = reader.GetDateTime(reader.GetOrdinal("CreatedOn")),

                UpdatedOn = reader.IsDBNull(reader.GetOrdinal("UpdatedOn"))
                    ? (DateTime?)null
                    : reader.GetDateTime(reader.GetOrdinal("UpdatedOn"))
            };
        }

        // datapoint transalations


      

        public async Task<DatapointTranslationDto?> GetTranslationByDataPointIdAsync(int dataPointId)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<DatapointTranslationDto>> GetAllTranslationsAsync(int dataPointId)
        {
            try
            {
                const string query = @"
            SELECT AttribubteId, LanguageCode, Text, description
            FROM tbl_DataPoint_Translations
            WHERE AttribubteId = @DataPointId";

                var list = new List<DatapointTranslationDto>();

                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@DataPointId", dataPointId);

                await conn.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(new DatapointTranslationDto
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("AttribubteId")),

                        LanguageCode = reader.IsDBNull(reader.GetOrdinal("LanguageCode"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("LanguageCode")),
                        Text = reader.IsDBNull(reader.GetOrdinal("Text"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("Text"))
                    });
                }
                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetcing datapoint transalations for datapoint {Id}", dataPointId);
                throw;
            }
           
        }

        public async Task AddOrUpdateTranslationAsync(
      DatapointTranslationDto translation,
      string userId)
        {
            _logger.LogInformation(
                "AddOrUpdateTranslation started. DataPointId={DataPointId}, TranslationId={TranslationId}, LanguageCode={LanguageCode}, UserId={UserId}",
                translation.DataPointId,
                translation.Id,
                translation.LanguageCode,
                userId);

            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                if (translation.Id == 0)
                {
                    const string insertQuery = @"
                INSERT INTO tbl_DataPoint_Translations (AttribubteId, LanguageCode, Text)
                VALUES (@DataPointId, @LanguageCode, @Text);";

                    using var cmd = new SqlCommand(insertQuery, conn);
                    cmd.Parameters.AddWithValue("@DataPointId", translation.DataPointId);
                    cmd.Parameters.AddWithValue("@LanguageCode", (object?)translation.LanguageCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Text", (object?)translation.Text ?? DBNull.Value);

                    var rows = await cmd.ExecuteNonQueryAsync();

                    _logger.LogInformation(
                        "Inserted DataPoint translation. RowsAffected={Rows}, DataPointId={DataPointId}, LanguageCode={LanguageCode}",
                        rows,
                        translation.DataPointId,
                        translation.LanguageCode);
                }
                else
                {
                    const string updateQuery = @"
                UPDATE tbl_DataPoint_Translations
                SET Text = @Text
                WHERE AttribubteId = @Id
                  AND LanguageCode = @LanguageCode;";

                    using var cmd = new SqlCommand(updateQuery, conn);
                    cmd.Parameters.AddWithValue("@Id", translation.Id);
                    cmd.Parameters.AddWithValue("@LanguageCode", (object?)translation.LanguageCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Text", (object?)translation.Text ?? DBNull.Value);

                    var rows = await cmd.ExecuteNonQueryAsync();

                    _logger.LogInformation(
                        "Updated DataPoint translation. RowsAffected={Rows}, TranslationId={TranslationId}, LanguageCode={LanguageCode}",
                        rows,
                        translation.Id,
                        translation.LanguageCode);
                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(
                    sqlEx,
                    "SQL error while adding/updating DataPoint translation. DataPointId={DataPointId}, TranslationId={TranslationId}, LanguageCode={LanguageCode}, UserId={UserId}",
                    translation.DataPointId,
                    translation.Id,
                    translation.LanguageCode,
                    userId);

                throw; // preserve stack trace
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while adding/updating DataPoint translation. DataPointId={DataPointId}, TranslationId={TranslationId}, UserId={UserId}",
                    translation.DataPointId,
                    translation.Id,
                    userId);

                throw;
            }
        }


        public async Task DeleteTranslationAsync(int dataPointId, string languageCode)
        {
            const string query = @"DELETE FROM tbl_DataPoint_Translations WHERE AttribubteId = @AttribubteId and LanguageCode = @LanguageCode";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@AttribubteId", dataPointId);
            cmd.Parameters.AddWithValue("@LanguageCode", languageCode);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }


    }
}
