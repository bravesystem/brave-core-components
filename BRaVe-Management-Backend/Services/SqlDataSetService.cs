using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;

namespace BRaVe_Management_Backend.Services
{
    public class SqlDataSetService : IDatasetService
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlDataSetService> _logger;

        public SqlDataSetService(ISecretProvider secretProvider, ILogger<SqlDataSetService> logger)
        {
            _logger = logger;
            _connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection).Result;
        }

        public async Task<int> CreateAsync(DatasetsDto dataset, int tenantId, string userId)
        {
            const string sql = @"
                INSERT INTO dbo.tbl_Datasets
                    (TenantId, Title, Description, SchemaJson, CreatedBy)
                VALUES
                    (@TenantId, @Title, @Description, @SchemaJson, @CreatedBy);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await using var cmd = new SqlCommand(sql, conn);

                cmd.Parameters.AddWithValue("@TenantId", tenantId);
                cmd.Parameters.AddWithValue("@Title", dataset.Title);
                cmd.Parameters.AddWithValue("@Description", dataset.Description ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@SchemaJson", dataset.SchemaJson);
                cmd.Parameters.AddWithValue("@CreatedBy", userId ?? (object)DBNull.Value);

                await conn.OpenAsync();
                var newId = (int)await cmd.ExecuteScalarAsync();

                _logger.LogInformation("Dataset created successfully: {Title} (Id: {Id})", dataset.Title, newId);

                return newId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating dataset: {Title}", dataset.Title);
                throw;
            }
        }

        public async Task<bool> DeactivateAsync(int id, int tenantId, string userId)
        {
            const string sql = @"
                UPDATE dbo.tbl_Datasets
                SET IsActive = 0,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @Id AND TenantId = @TenantId;";

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await using var cmd = new SqlCommand(sql, conn);

                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@TenantId", tenantId);

                await conn.OpenAsync();
                var rows = await cmd.ExecuteNonQueryAsync();

                _logger.LogInformation("Dataset deactivated: Id {Id}, Tenant {TenantId}", id, tenantId);

                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating dataset Id {Id}", id);
                throw;
            }
        }

        public async Task<List<DatasetsDto>> GetAllAsync(int tenantId)
        {
            const string sql = @"
                SELECT Id, TenantId, Title, Description, SchemaJson, IsActive, Version, CreatedBy, CreatedAt, UpdatedAt
                FROM dbo.tbl_Datasets
                WHERE TenantId = @TenantId
                ORDER BY CreatedAt DESC;";

            var results = new List<DatasetsDto>();

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@TenantId", tenantId);

                await conn.OpenAsync();
                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new DatasetsDto
                    {
                        Id = reader.GetInt32("Id"),
                        TenantId = reader.GetInt32("TenantId"),
                        Title = reader.GetString("Title"),
                        Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                        SchemaJson = reader.GetString("SchemaJson"),
                        IsActive = reader.GetBoolean("IsActive"),
                        Version = reader.GetInt32("Version"),
                        CreatedBy = reader.IsDBNull("CreatedBy") ? null : reader.GetString("CreatedBy"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        UpdatedAt = reader.IsDBNull("UpdatedAt") ? null : reader.GetDateTime("UpdatedAt")
                    });
                }

                _logger.LogInformation("{Count} datasets retrieved for tenant {TenantId}", results.Count, tenantId);

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving datasets for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<DatasetsDto?> GetByIdAsync(int id, int tenantId)
        {
            const string sql = @"
                SELECT Id, TenantId, Title, Description, SchemaJson, IsActive, Version, CreatedBy, CreatedAt, UpdatedAt
                FROM dbo.tbl_Datasets
                WHERE Id = @Id AND TenantId = @TenantId;";

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await using var cmd = new SqlCommand(sql, conn);

                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@TenantId", tenantId);

                await conn.OpenAsync();
                await using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var dataset = new DatasetsDto
                    {
                        Id = reader.GetInt32("Id"),
                        TenantId = reader.GetInt32("TenantId"),
                        Title = reader.GetString("Title"),
                        Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                        SchemaJson = reader.GetString("SchemaJson"),
                        IsActive = reader.GetBoolean("IsActive"),
                        Version = reader.GetInt32("Version"),
                        CreatedBy = reader.IsDBNull("CreatedBy") ? null : reader.GetString("CreatedBy"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        UpdatedAt = reader.IsDBNull("UpdatedAt") ? null : reader.GetDateTime("UpdatedAt")
                    };

                    _logger.LogInformation("Dataset retrieved: Id {Id}, Tenant {TenantId}", id, tenantId);

                    return dataset;
                }

                _logger.LogWarning("Dataset not found: Id {Id}, Tenant {TenantId}", id, tenantId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dataset Id {Id}", id);
                throw;
            }
        }

        public async Task<bool> UpdateAsync(DatasetsDto dataset, int tenantId, string userId)
        {
            const string sql = @"
                UPDATE dbo.tbl_Datasets
                SET Title = @Title,
                    Description = @Description,
                    SchemaJson = @SchemaJson,
                    Version = Version + 1,
                    IsActive=1,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @Id AND TenantId = @TenantId;";

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await using var cmd = new SqlCommand(sql, conn);

                cmd.Parameters.AddWithValue("@Id", dataset.Id);
                cmd.Parameters.AddWithValue("@TenantId", tenantId);
                cmd.Parameters.AddWithValue("@Title", dataset.Title);
                cmd.Parameters.AddWithValue("@Description", dataset.Description ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@SchemaJson", dataset.SchemaJson);

                await conn.OpenAsync();
                var rows = await cmd.ExecuteNonQueryAsync();

                _logger.LogInformation("Dataset updated: Id {Id}, Tenant {TenantId}", dataset.Id, tenantId);

                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating dataset Id {Id}", dataset.Id);
                throw;
            }
        }
    }
}
