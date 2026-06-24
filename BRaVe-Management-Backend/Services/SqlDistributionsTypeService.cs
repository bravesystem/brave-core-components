using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Data.SqlClient;
using System.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BRaVe_Management_Backend.Services
{
    public class SqlDistributionsTypeService : IDistributionTypesService
    {
        private string connectionString { get; set; }
        private readonly ILogger<SqlDistributionsTypeService> logger;
        public SqlDistributionsTypeService(
     ILogger<SqlDistributionsTypeService> _logger,
     ISecretProvider secretProvider)
        {
            logger = _logger;

            connectionString = secretProvider
                .GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection)
                .GetAwaiter()
                .GetResult();

            logger.LogInformation("SqlDistributionsTypeService initialized");
        }

        public Task UpdateDistributionTypes(DistributionTypes data, string UserId)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<DistributionTypes>> GetAllDistributionTypes(int tenantId)
        {
            try
            {
                logger.LogInformation("Fetching Distribution Types for Tenant {TenantId}", tenantId);

                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("sp_GetDistributionTypes", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@TenantId", tenantId);

                List<DistributionTypes> results = new();

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var distribution = new DistributionTypes
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        TenantId = reader.GetInt32(reader.GetOrdinal("TenantId")),
                        Title = reader.IsDBNull(reader.GetOrdinal("Title")) ? null : reader.GetString(reader.GetOrdinal("Title")),
                        Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                        IsActive = reader.IsDBNull(reader.GetOrdinal("IsActive")) ? null : reader.GetBoolean(reader.GetOrdinal("IsActive")),
                        ExternalId = reader.IsDBNull(reader.GetOrdinal("ExternalId")) ? null : reader.GetString(reader.GetOrdinal("ExternalId")),
                        CreatedByUserId = reader.GetString(reader.GetOrdinal("CreatedByUserId")),
                        CreatedOn = reader.IsDBNull(reader.GetOrdinal("CreatedOn")) ? null : reader.GetDateTime(reader.GetOrdinal("CreatedOn")),
                        UpdatedByUserId = reader.IsDBNull(reader.GetOrdinal("UpdatedByUserId")) ? null : reader.GetString(reader.GetOrdinal("UpdatedByUserId")),
                        UpdatedOn = reader.IsDBNull(reader.GetOrdinal("UpdatedOn")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedOn")),
                        unitCount = reader.GetInt32(reader.GetOrdinal("units"))
                    };

                    results.Add(distribution);
                }

                logger.LogInformation("Fetched {Count} Distribution Types for Tenant {TenantId}", results.Count, tenantId);

                return results;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving distribution types for tenant={TenantId}", tenantId);
                return Enumerable.Empty<DistributionTypes>();
            }
        }
        public Task<int?> GetAllDistributionKitItemss(int tenantId)
        {
            throw new NotImplementedException();
        }

        public Task<int?> GetAllDistributionItems(int tenantId)
        {
            throw new NotImplementedException();
        }

        public Task<DistributionTypes?> GetDistributionTypesById(int id)
        {
            throw new NotImplementedException();
        }

        public async Task UpdateDistributionType(int id, DistributionTypes dto)
        {
            try
            {
                logger.LogInformation("Updating DistributionType Id {Id} for Tenant {TenantId}", id, dto.TenantId);

                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("sp_UpdateDistributionType", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@TenantId", dto.TenantId);
                cmd.Parameters.AddWithValue("@Title", dto.Title);
                cmd.Parameters.AddWithValue("@Description", (object?)dto.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ExternalId", (object?)dto.ExternalId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IsActive", dto.IsActive);
                cmd.Parameters.AddWithValue("@UpdatedByUserId", dto.UpdatedByUserId);

                logger.LogInformation("Executing sp_UpdateDistributionType for Id {Id}", id);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var status = reader["Status"]?.ToString();
                    var message = reader["Message"]?.ToString();

                    logger.LogInformation("SP Result - Status: {Status}, Message: {Message}", status, message);

                    if (status != "Success")
                    {
                        logger.LogError("Failed to update DistributionType Id {Id}: {Message}", id, message);
                        throw new Exception(message);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating DistributionType Id {Id}", id);
                throw;
            }
        }

        public async Task AddDistributionType(DistributionTypes dto)
        {
            try
            {
                logger.LogInformation("Creating DistributionType for Tenant {TenantId}", dto.TenantId);

                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("sp_CreateDistributionType", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@TenantId", dto.TenantId);
                cmd.Parameters.AddWithValue("@Title", dto.Title);
                cmd.Parameters.AddWithValue("@Description", (object?)dto.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ExternalId", (object?)dto.ExternalId ?? DBNull.Value);
                //cmd.Parameters.AddWithValue("@IsActive", dto.IsActive);
                cmd.Parameters.AddWithValue("@CreatedByUserId", dto.CreatedByUserId);

                logger.LogInformation("Executing sp_CreateDistributionType for Title {Title}", dto.Title);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var status = reader["Status"]?.ToString();
                    var message = reader["Message"]?.ToString();

                    logger.LogInformation("SP Result - Status: {Status}, Message: {Message}", status, message);

                    if (status != "Success")
                    {
                        logger.LogError("Failed to create DistributionType: {Message}", message);
                        throw new Exception(message);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating DistributionType {Title}", dto.Title);
                throw;
            }
        }
    }
}
