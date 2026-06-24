using Azure.Core;
using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using static Lucene.Net.Util.Fst.Util;

namespace BRaVe_Management_Backend.Services
{
    public class SqlDistributionsCompositionService : IDistributionCompositionService
    {
        private readonly string connectionString;
        private readonly ILogger<SqlDistributionsCompositionService> _logger;

        public SqlDistributionsCompositionService(
            ISecretProvider secretProvider,
            ILogger<SqlDistributionsCompositionService> logger)
        {
            connectionString = secretProvider
                .GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection)
                .GetAwaiter()
                .GetResult();

            _logger = logger;
        }

        public async Task<IEnumerable<DistributionComposition>> GetAllDistributionComposition(int distributionId, int tenantId)
        {
            var result = new List<DistributionComposition>();

            try
            {
                _logger.LogInformation(
                    "Fetching distribution items. DistributionId:{DistributionId} TenantId:{TenantId}",
                    distributionId, tenantId);

                using var conn = new SqlConnection(connectionString);
                using var cmd = new SqlCommand("sp_GetDistributionItems", conn);

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@DistributionId", distributionId);
                cmd.Parameters.AddWithValue("@TenantId", tenantId);

                await conn.OpenAsync();

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    result.Add(new DistributionComposition
                    {
                        DistributionId = reader.GetInt32(reader.GetOrdinal("DistributionId")),
                        TenantId = reader.GetInt32(reader.GetOrdinal("TenantId")),

                        KitTypeId = reader.IsDBNull(reader.GetOrdinal("KitTypeId"))
    ? (long?)null
    : reader.GetInt64(reader.GetOrdinal("KitTypeId")),

                        ItemTypeId = reader.IsDBNull(reader.GetOrdinal("ItemTypeId"))
    ? (long?)null
    : reader.GetInt64(reader.GetOrdinal("ItemTypeId")),

                        UnitTitle = reader["UnitTitle"]?.ToString(),

                        IsActive = !reader.IsDBNull(reader.GetOrdinal("IsActive")) &&
                                   reader.GetBoolean(reader.GetOrdinal("IsActive")),

                        TargetType = reader["TargetType"]?.ToString(),

                        Quantity = reader.IsDBNull(reader.GetOrdinal("Quantity"))
                            ? 0
                            : reader.GetInt32(reader.GetOrdinal("Quantity")),

                        QRCode = reader["QRCode"]?.ToString(),

                        SKU = reader["SKU"]?.ToString(),

                        Measure = reader.IsDBNull(reader.GetOrdinal("Measure"))
                            ? null
                            : reader.GetDecimal(reader.GetOrdinal("Measure")),

                        UoM = reader["UoM"]?.ToString(),
                        ItemCount = reader.GetInt32(reader.GetOrdinal("units")),

                    });
                }

                _logger.LogInformation(
                    "Distribution items fetched successfully. Count:{Count}",
                    result.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error fetching distribution items. DistributionId:{DistributionId}",
                    distributionId);

                throw;
            }

            return result;
        }

        public async Task<IEnumerable<DistributionComposition>> GetDistributionCompositionsAsync(int tenantId)
        {
            //List<DistributionComposition> results = new();
            var result = new List<DistributionComposition>();

            try
            {
                _logger.LogInformation("Fetching Distribution Compositions for TenantId: {TenantId}", tenantId);

                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("sp_GetDistributionCompositions", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@TenantId", tenantId);

                using var reader = await cmd.ExecuteReaderAsync();


                while (await reader.ReadAsync())
                {
                    result.Add(new DistributionComposition
                    {
                        DistributionId = reader.GetInt32(reader.GetOrdinal("DistributionId")),
                        TenantId = reader.GetInt32(reader.GetOrdinal("TenantId")),

                        UnitTitle = reader["UnitTitle"]?.ToString(),

                        IsActive = !reader.IsDBNull(reader.GetOrdinal("IsActive")) &&
                                   reader.GetBoolean(reader.GetOrdinal("IsActive")),

                        TargetType = reader["TargetType"]?.ToString(),

                        Quantity = reader.IsDBNull(reader.GetOrdinal("Quantity"))
                            ? 0
                            : reader.GetInt32(reader.GetOrdinal("Quantity")),

                        QRCode = reader["QRCode"]?.ToString(),

                        SKU = reader["SKU"]?.ToString(),

                        Measure = reader.IsDBNull(reader.GetOrdinal("Measure"))
                            ? null
                            : reader.GetDecimal(reader.GetOrdinal("Measure")),

                        UoM = reader["UoM"]?.ToString()
                    });
                }

                _logger.LogInformation(
                    "Fetched {Count} Distribution Composition rows for TenantId: {TenantId}",
                    result.Count, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error retrieving Distribution Compositions for TenantId: {TenantId}",
                    tenantId);
                throw;
            }

            return result;
        }

        public async Task<IEnumerable<DistributionItemTypes>> GetDistributionItemsAsync(int tenantId)
        {
            var results = new List<DistributionItemTypes>();

            try
            {
                _logger.LogInformation(
                    "Fetching Distribution Item Types for TenantId:{TenantId}",
                    tenantId);

                using var conn = new SqlConnection(connectionString);
                using var cmd = new SqlCommand("sp_GetDistributionItemTypes", conn);

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@TenantId", tenantId);

                await conn.OpenAsync();

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new DistributionItemTypes
                    {
                        Id = reader.GetInt64(reader.GetOrdinal("Id")),
                        TenantId = reader.GetInt32(reader.GetOrdinal("TenantId")),

                        Name = reader["Name"]?.ToString(),
                        Notes = reader["Notes"]?.ToString(),
                        Source = reader["Source"]?.ToString(),

                        SKU = reader["SKU"]?.ToString(),
                        QRCode = reader["QRCode"]?.ToString(),
                        ExternalId = reader["ExternalId"]?.ToString(),

                        IsActive = !reader.IsDBNull(reader.GetOrdinal("IsActive")) &&
                                   reader.GetBoolean(reader.GetOrdinal("IsActive")),

                        CreatedByUserId = reader["CreatedByUserId"]?.ToString(),

                        CreatedOn = reader.IsDBNull(reader.GetOrdinal("CreatedOn"))
                            ? DateTime.MinValue
                            : reader.GetDateTime(reader.GetOrdinal("CreatedOn")),

                        UpdatedByUserId = reader["UpdatedByUserId"]?.ToString(),

                        UpdatedOn = reader.IsDBNull(reader.GetOrdinal("UpdatedOn"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("UpdatedOn"))
                    });
                }

                _logger.LogInformation(
                    "Fetched {Count} Distribution Item Types for TenantId:{TenantId}",
                    results.Count, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error retrieving Distribution Item Types for TenantId:{TenantId}",
                    tenantId);

                throw;
            }

            return results;
        }

        public async Task<long> CreateDistributionItemAsync(DistributionItemTypes item)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);

                using var cmd = new SqlCommand("sp_CreateItem", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@TenantId", SqlDbType.Int).Value = item.TenantId;
                cmd.Parameters.Add("@SKU", SqlDbType.NVarChar, 50).Value = (object?)item.SKU ?? DBNull.Value;
                cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = item.Name;
                cmd.Parameters.Add("@Notes", SqlDbType.NVarChar, 500).Value = (object?)item.Notes ?? DBNull.Value;
                cmd.Parameters.Add("@Source", SqlDbType.VarChar, 1).Value = item.Source;
                cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = item.IsActive;
                cmd.Parameters.Add("@ExternalId", SqlDbType.NVarChar, 100).Value = (object?)item.ExternalId ?? DBNull.Value;
                cmd.Parameters.Add("@QRCode", SqlDbType.NVarChar, 500).Value = (object?)item.QRCode ?? DBNull.Value;
                cmd.Parameters.Add("@CreatedByUserId", SqlDbType.VarChar, 50).Value = item.CreatedByUserId;

                await conn.OpenAsync();

                long newId = 0;

                using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    if (reader["Status"].ToString() == "Success")
                    {
                        newId = Convert.ToInt64(reader["ItemId"]);
                    }
                }

                return newId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing sp_CreateItem");
                throw;
            }
        }

        public async Task UpdateDistributionItemAsync(DistributionItemTypes item)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);

                using var cmd = new SqlCommand("sp_UpdateDistributionItem", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@Id", SqlDbType.BigInt).Value = item.Id;
                cmd.Parameters.Add("@TenantId", SqlDbType.Int).Value = item.TenantId;
                cmd.Parameters.Add("@Name", SqlDbType.NVarChar).Value = item.Name ?? (object)DBNull.Value;
                cmd.Parameters.Add("@Notes", SqlDbType.NVarChar).Value = item.Notes ?? (object)DBNull.Value;
                cmd.Parameters.Add("@Source", SqlDbType.VarChar).Value = item.Source ?? "I";
                cmd.Parameters.Add("@SKU", SqlDbType.NVarChar).Value = item.SKU ?? (object)DBNull.Value;
                cmd.Parameters.Add("@QRCode", SqlDbType.NVarChar).Value = item.QRCode ?? (object)DBNull.Value;
                cmd.Parameters.Add("@ExternalId", SqlDbType.NVarChar).Value = item.ExternalId ?? (object)DBNull.Value;
                cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = item.IsActive;

                await conn.OpenAsync();

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating distribution item {Id}", item.Id);
                throw;
            }
        }

        public async Task AddDistributionItemsAsync(List<DistributionItemCreateDto> dtoList)
        {
            if (dtoList == null || !dtoList.Any())
                return;

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            foreach (var dto in dtoList)
            {
                try
                {
                    using var cmd = new SqlCommand("dbo.sp_AddDistributionItem", conn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.AddWithValue("@TenantId", dto.TenantId);
                    cmd.Parameters.AddWithValue("@DistributionTypeId", dto.DistributionTypeId);
                    cmd.Parameters.AddWithValue("@ItemTypeId", dto.ItemTypeId);
                    cmd.Parameters.AddWithValue("@Quantity", dto.Quantity);
                    cmd.Parameters.AddWithValue("@Measure", dto.Measure);
                    cmd.Parameters.AddWithValue("@UoM", dto.UoM);

                    await cmd.ExecuteNonQueryAsync();
                }
                catch (SqlException ex)
                {
                    // Capture SP errors, including RAISERROR messages
                    _logger.LogError(ex,
                        "Error adding item {ItemTypeId} to distribution {DistributionTypeId}",
                        dto.ItemTypeId, dto.DistributionTypeId);

                    // Wrap and rethrow with context
                    throw new InvalidOperationException(
                        $"Error adding item {dto.ItemTypeId} to distribution {dto.DistributionTypeId}: {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Unexpected error adding item {ItemTypeId} to distribution {DistributionTypeId}",
                        dto.ItemTypeId, dto.DistributionTypeId);
                    throw;
                }
            }
        }

        public async Task AddDistributionKitsAsync(List<DistributionKitCreateDto> kits)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                using var cmd = new SqlCommand("sp_AddDistributionKits", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                // Build DataTable matching SQL type
                var table = new DataTable();
                table.Columns.Add("TenantId", typeof(int));
                table.Columns.Add("DistributionTypeId", typeof(int));
                table.Columns.Add("KitTypeId", typeof(long));
                table.Columns.Add("Quantity", typeof(int));

                foreach (var kit in kits)
                {
                    table.Rows.Add(
                        kit.TenantId,
                        kit.DistributionTypeId,
                        kit.KitTypeId,
                        kit.Quantity
                    );
                }

                var param = cmd.Parameters.AddWithValue("@Kits", table);
                param.SqlDbType = SqlDbType.Structured;
                param.TypeName = "dbo.DistributionKitType";

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw; // don’t use throw ex;
            }
        }

        public async Task<List<KitTypeItem>> GetAllKitItemsAsync()
        {
            var results = new List<KitTypeItem>();

            using var conn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("sp_GetKitItems", conn);

            cmd.CommandType = CommandType.StoredProcedure;
        

            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                results.Add(new KitTypeItem
                {
                    KitId = reader.GetInt64(reader.GetOrdinal("KitId")),
                    ItemId = reader.GetInt64(reader.GetOrdinal("ItemId")),
                    TenantId = reader.GetInt32(reader.GetOrdinal("TenantId")),
                    Measure = reader.GetDecimal(reader.GetOrdinal("Measure")),
                    UoM = reader.GetInt32(reader.GetOrdinal("UoM")),
                    CreatedByUserId = reader.GetString(reader.GetOrdinal("CreatedByUserId")),
                    CreatedOn = reader.IsDBNull(reader.GetOrdinal("CreatedOn"))
                                ? null
                                : reader.GetDateTime(reader.GetOrdinal("CreatedOn")),
                    UpdatedByUserId = reader.IsDBNull(reader.GetOrdinal("UpdatedByUserId"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("UpdatedByUserId")),
                    UpdatedOn = reader.IsDBNull(reader.GetOrdinal("UpdatedOn"))
                                ? null
                                : reader.GetDateTime(reader.GetOrdinal("UpdatedOn"))
                });
            }

            return results;
        }
    
        public async Task<List<KitItemsDto>> GetKitItemByKitIdAsync(long kitId, int tenantId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@KitId", kitId);
            parameters.Add("@TenantId", tenantId);

            using var connection = new SqlConnection(connectionString);

            var result = await connection.QueryAsync<KitItemsDto>(
                "sp_GetKitItemsByKitId",
                parameters,
                commandType: CommandType.StoredProcedure);

            return result.ToList();
        }

        public async Task AddKitItemsAsync(int tenantId, string createdByUserId, List<KitItemInsertDto> items)
        {
            if (items == null || !items.Any())
                return;

            try
            {
                var kitId = items.First().KitId;

                var table = new DataTable();
                table.Columns.Add("ItemId", typeof(long));
                table.Columns.Add("Measure", typeof(decimal));
                table.Columns.Add("UoM", typeof(int));

                foreach (var item in items)
                {
                    table.Rows.Add(item.ItemId, item.Measure, item.UoM);
                }

                using var conn = new SqlConnection(connectionString);
                using var cmd = new SqlCommand("dbo.sp_AddKitItems", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@KitId", SqlDbType.BigInt).Value = kitId;
                cmd.Parameters.Add("@TenantId", SqlDbType.Int).Value = tenantId;
                cmd.Parameters.Add("@CreatedByUserId", SqlDbType.VarChar, 50).Value = createdByUserId;

                var tvpParam = cmd.Parameters.AddWithValue("@Items", table);
                tvpParam.SqlDbType = SqlDbType.Structured;
                tvpParam.TypeName = "dbo.KitItemType";

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk inserting kit items");
                throw;
            }
        }

        public async Task UpdateInline(int id, decimal measure, int uoM)
        {
            try
            {

                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var cmd = new SqlCommand(@"UPDATE tbl_KitTypeItems SET Measure = @Measure, UoM = @UoM,  UpdatedOn = GETDATE() WHERE ItemId = @Id", conn);
                cmd.Parameters.AddWithValue("@Measure", measure);

                cmd.Parameters.AddWithValue("@UoM", uoM);
  
                cmd.Parameters.AddWithValue("@Id", id);
               // cmd.Parameters.AddWithValue("@UpdatedByUserId", );
                await cmd.ExecuteNonQueryAsync();
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while Updating Kit Item Type '{id}'", id);
                throw;
            }
        }
        public async Task UpdateInlineItemDistribution(int id, DistributionItemCreateDto dto)
        {
            _logger.LogInformation("Starting inline update for ItemTypeId {Id}", id);

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand(@"
            UPDATE tbl_DistributionTypeLinks 
            SET 
                Measure = @Measure, 
                UoM = @UoM,  
                Quantity = @Quantity
            WHERE ItemTypeId = @Id", conn);

                cmd.CommandType = CommandType.Text;

                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@Measure", dto.Measure);
                cmd.Parameters.AddWithValue("@UoM", dto.UoM);
                cmd.Parameters.AddWithValue("@Quantity", dto.Quantity);

                var rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected == 0)
                {
                    _logger.LogWarning("Inline update failed. No rows affected for ItemTypeId {Id}", id);
                    throw new Exception($"No record updated for ItemTypeId {id}");
                }

                _logger.LogInformation("Inline update successful for ItemTypeId {Id}. Rows affected: {Rows}", id, rowsAffected);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during inline update for ItemTypeId {Id}", id);
                throw;
            }
        }
        public async Task UpdateInlineKitDistribution(DistributionKitCreateDto dto)
        {
            _logger.LogInformation("Starting inline update for ItemTypeId {Id}", dto.DistributionTypeId);

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand(@"
            UPDATE tbl_DistributionTypeLinks 
            SET 
             
                Quantity = @Quantity
            WHERE KitTypeId = @KitTypeId AND DistributionTypeId=@DistributionTypeId", conn);

                cmd.CommandType = CommandType.Text;

                cmd.Parameters.AddWithValue("@KitTypeId", dto.KitTypeId);
                cmd.Parameters.AddWithValue("@DistributionTypeId", dto.DistributionTypeId);
                cmd.Parameters.AddWithValue("@Quantity", dto.Quantity);

                var rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected == 0)
                {
                    _logger.LogWarning("Inline update failed. No rows affected for ItemTypeId {Id}", dto.DistributionTypeId);
                    throw new Exception($"No record updated for ItemTypeId {dto.DistributionTypeId}");
                }

                _logger.LogInformation("Inline update successful for ItemTypeId {Id}. Rows affected: {Rows}", dto.DistributionTypeId, rowsAffected);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during inline update for ItemTypeId {Id}", dto.DistributionTypeId);
                throw;
            }
        }
    }
}