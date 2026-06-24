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
    public class SqlDistributionKitTypeService : IDistributionKitTypeService
    {
        private string connectionString { get; set; }
        private readonly ILogger<SqlDistributionKitTypeService> _logger;


        public SqlDistributionKitTypeService(ISecretProvider secretProvider, ILogger<SqlDistributionKitTypeService> logger ) {

            connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection).Result;
            _logger = logger;
            //config.GetConnectionString("DefaultConnection");
        }
 
     
 
        public async Task<IEnumerable<KitType>> GetAllKitTypes(int tenantId)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("sp_GetAllKitTypes", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@TenantId", SqlDbType.Int).Value = tenantId;

                var kitTypes = new List<KitType>();

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var kit = new KitType
                    {
                        Id = reader.GetInt64(reader.GetOrdinal("Id")),

                        ItemCount = reader.GetInt32(reader.GetOrdinal("ItemCount")),

                        TenantId = reader.GetInt32(reader.GetOrdinal("TenantId")),

                        SKU = reader.IsDBNull(reader.GetOrdinal("SKU"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("SKU")),

                        Name = reader.GetString(reader.GetOrdinal("Name")),

                        Notes = reader.IsDBNull(reader.GetOrdinal("Notes"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("Notes")),

                        Source = reader.GetString(reader.GetOrdinal("Source")),

                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),

                        ExternalId = reader.IsDBNull(reader.GetOrdinal("ExternalId"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("ExternalId")),

                        QRCode = reader.IsDBNull(reader.GetOrdinal("QRCode"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("QRCode")),

                        CreatedByUserId = reader.GetString(reader.GetOrdinal("CreatedByUserId")),

                        CreatedOn = reader.GetDateTime(reader.GetOrdinal("CreatedOn")),

                        UpdatedByUserId = reader.IsDBNull(reader.GetOrdinal("UpdatedByUserId"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("UpdatedByUserId")),

                        UpdatedOn = reader.IsDBNull(reader.GetOrdinal("UpdatedOn"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("UpdatedOn"))
                    };

                    kitTypes.Add(kit);
                }

                return kitTypes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving kit types with tenantId={TenantId}", tenantId);
            }

            return Enumerable.Empty<KitType>();
        }

        public async Task<IEnumerable<KitType>> GetAllKitTypesWithItems(int tenantId)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("sp_GetAllKitTypes", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@TenantId", SqlDbType.Int).Value = tenantId;

                var kitTypes = new List<KitType>();

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var kit = new KitType
                    {
                        Id = reader.GetInt64(reader.GetOrdinal("Id")),

                        ItemCount = reader.GetInt32(reader.GetOrdinal("ItemCount")),

                        TenantId = reader.GetInt32(reader.GetOrdinal("TenantId")),

                        SKU = reader.IsDBNull(reader.GetOrdinal("SKU"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("SKU")),

                        Name = reader.GetString(reader.GetOrdinal("Name")),

                        Notes = reader.IsDBNull(reader.GetOrdinal("Notes"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("Notes")),

                        Source = reader.GetString(reader.GetOrdinal("Source")),

                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),

                        ExternalId = reader.IsDBNull(reader.GetOrdinal("ExternalId"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("ExternalId")),

                        QRCode = reader.IsDBNull(reader.GetOrdinal("QRCode"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("QRCode")),

                        CreatedByUserId = reader.GetString(reader.GetOrdinal("CreatedByUserId")),

                        CreatedOn = reader.GetDateTime(reader.GetOrdinal("CreatedOn")),

                        UpdatedByUserId = reader.IsDBNull(reader.GetOrdinal("UpdatedByUserId"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("UpdatedByUserId")),

                        UpdatedOn = reader.IsDBNull(reader.GetOrdinal("UpdatedOn"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("UpdatedOn"))
                    };

                    kitTypes.Add(kit);
                }

                return kitTypes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving kit types with tenantId={TenantId}", tenantId);
            }

            return Enumerable.Empty<KitType>();
        }


        public async Task<long> CreateKitAsync(KitType kit)
        {
            _logger.LogInformation(
                "CreateKitAsync started. TenantId:{TenantId} Name:{Name} SKU:{SKU} Source:{Source}",
                kit.TenantId, kit.Name, kit.SKU, kit.Source);

            try
            {
                using var conn = new SqlConnection(connectionString);

                var cmd = new SqlCommand("sp_CreateKitType", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@TenantId", kit.TenantId);
                cmd.Parameters.AddWithValue("@Name", kit.Name);
                cmd.Parameters.AddWithValue("@SKU", kit.SKU ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Source", kit.Source);
                cmd.Parameters.AddWithValue("@Notes", kit.Notes ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ExternalId", kit.ExternalId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@QRCode", kit.QRCode ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@IsActive", kit.IsActive);
                cmd.Parameters.AddWithValue("@CreatedByUserId", kit.CreatedByUserId ?? (object)DBNull.Value);
                //cmd.Parameters.AddWithValue("@CreatedOn", kit.CreatedOn);

                _logger.LogInformation("Opening SQL connection for CreateKitAsync");

                await conn.OpenAsync();

                var result = Convert.ToInt64(await cmd.ExecuteScalarAsync());

                _logger.LogInformation(
                    "CreateKitAsync completed successfully. New KitId:{KitId}", result);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred while creating kit. TenantId:{TenantId} Name:{Name}",
                    kit.TenantId,
                    kit.Name);

                throw;
            }
        }
        public async Task UpdateKitAsync(KitType kit)
        {
            _logger.LogInformation(
                "UpdateKitAsync started. KitId:{Id} Name:{Name}", kit.Id, kit.Name);

            try
            {
                using var conn = new SqlConnection(connectionString);

                var cmd = new SqlCommand("sp_UpdateKitType", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Id", kit.Id);
                cmd.Parameters.AddWithValue("@Name", kit.Name);
                cmd.Parameters.AddWithValue("@SKU", kit.SKU ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Source", kit.Source);
                cmd.Parameters.AddWithValue("@Notes", kit.Notes ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ExternalId", kit.ExternalId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@QRCode", kit.QRCode ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@IsActive", kit.IsActive);
                cmd.Parameters.AddWithValue("@UpdatedByUserId", kit.UpdatedByUserId ?? (object)DBNull.Value);
                //cmd.Parameters.AddWithValue("@UpdatedOn", kit.UpdatedOn ?? DateTime.UtcNow);

                _logger.LogInformation("Opening SQL connection for UpdateKitAsync");

                await conn.OpenAsync();

                var rows = await cmd.ExecuteNonQueryAsync();

                _logger.LogInformation(
                    "UpdateKitAsync completed. KitId:{Id} RowsAffected:{Rows}",
                    kit.Id,
                    rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred while updating kit. KitId:{Id}",
                    kit.Id);

                throw;
            }
        }

     
    }
}
