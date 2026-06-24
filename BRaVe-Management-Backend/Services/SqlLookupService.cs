using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using Serilog;
using ClosedXML.Excel;

namespace BRaVe_Management_Backend.Services
{
    public class SqlLookupService : ILookupService
    {
        private readonly string _connectionString;
        private static readonly Serilog.ILogger _logger = Log.ForContext<SqlLookupService>();

        public SqlLookupService(ISecretProvider secretProvider)
        {
            _connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection).Result;
            _logger.Information("SqlLookupService initialized. Connection string successfully retrieved from KeyVault.");
        }

        public async Task<CoreLookups> GetCoreLookups(string lang = null)
        {
            var result = new CoreLookups();
            _logger.Information("Fetching core lookups. Language: {Language}", lang ?? "default");

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetCoreLookups", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@LanguageCode", (object?)lang ?? DBNull.Value);

            try
            {
                await connection.OpenAsync();
                _logger.Debug("SQL connection opened successfully.");

                using var reader = await command.ExecuteReaderAsync();
                _logger.Debug("Stored procedure sp_GetCoreLookups executed successfully.");

                do
                {
                    while (await reader.ReadAsync())
                    {
                        var item = new LookupItemDto
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Code = reader.IsDBNull(reader.GetOrdinal("Code")) ? null : reader.GetString(reader.GetOrdinal("Code")),
                            Text = reader.GetString(reader.GetOrdinal("Text")),
                            LookupName = reader.GetString(reader.GetOrdinal("LookupName"))
                        };

                        UpdateRelatedLookup(result, item);
                    }

                } while (await reader.NextResultAsync());

                _logger.Information("Successfully loaded all core lookups.");
                return result;
            }
            catch (SqlException ex)
            {
                _logger.Error(ex, "SQL error occurred while executing GetCoreLookups (lang: {Language})", lang);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unexpected error occurred while fetching core lookups.");
                throw;
            }
        }

        private void UpdateRelatedLookup(CoreLookups result, LookupItemDto item)
        {
            _logger.Debug("Processing lookup item {LookupName} (Id: {Id})", item.LookupName, item.Id);

            switch (item.LookupName)
            {
                case "Roles": result.Roles.Add(item); break;
                case "FocalPoints": result.FocalPoints.Add(item); break;
                case "AssessmentStatuses": result.AssessmentStatuses.Add(item); break;
                case "AssessmentPurposeOfProcessing": result.AssessmentPurposeOfProcessing.Add(item); break;
                case "PartnerOganizations": result.PartnerOganizations.Add(item); break;
                case "HouseholdTypes": result.HouseholdTypes.Add(item); break;
                case "PersonalDataCategories": result.PersonalDataCategories.Add(item); break;
                case "LawfulBases": result.LawfulBases.Add(item); break;
                case "DataDisclosureRecipients": result.DataDisclosureRecipients.Add(item); break;
                case "DataSharingRecipients": result.DataSharingRecipients.Add(item); break;
                case "TypeOfAgreements": result.TypeOfAgreements.Add(item); break;
                case "StatusOfAgreements": result.StatusOfAgreements.Add(item); break;
                case "ArchivingMethodTypes": result.ArchivingMethodTypes.Add(item); break;
                case "DisposalMethodTypes": result.DisposalMethodTypes.Add(item); break;
                case "SecurityMeasures": result.SecurityMeasures.Add(item); break;
                case "SourceOfData": result.SourceOfData.Add(item); break;
                case "DataOutputs": result.DataOutputs.Add(item); break;
                case "DataOutputRecipients": result.DataOutputRecipients.Add(item); break;
                case "ApprovalStatuses": result.ApprovalStatuses.Add(item); break;
                case "RecommendationImplementationStatuses": result.RecommendationImplementationStatuses.Add(item); break;
                case "CommonStatuses": result.CommonStatuses.Add(item); break;
                case "QuestionTypes": result.QuestionTypes.Add(item); break;
                case "SurveyTypes": result.SurveyTypes.Add(item); break;
                case "PreferenceTypes": result.PreferenceTypes.Add(item); break;
                case "UOM": result.UOM.Add(item); break;
                default:
                    _logger.Warning("Unhandled LookupName type encountered: {LookupName}", item.LookupName);
                    break;
            }
        }




        /// UPDATED. Uploads an Excel file containing lookup values and names and updates the database via a stored procedure.
        public async Task<bool> UploadLookupExcelV2Async(IFormFile excelFile, string userId, int tenantId)
        {
            _logger.Information("Starting lookup Excel V2 upload for user {UserId}, tenant {TenantId}", userId, tenantId);

            if (excelFile == null || excelFile.Length == 0)
            {
                _logger.Warning("No Excel file provided.");
                throw new ArgumentException("No file uploaded.");
            }

            // ------------------ 1) PREPARE DATATABLES ------------------

            var dtNames = new DataTable();
            dtNames.Columns.AddRange(new[]
            {
        new DataColumn("LookupName", typeof(string)),
        new DataColumn("Exists", typeof(bool))
    });

            var dtValues = new DataTable();
            dtValues.Columns.AddRange(new[]
            {
        new DataColumn("LookupName", typeof(string)),
        new DataColumn("ItemId", typeof(int)),
        new DataColumn("ItemName", typeof(string)),
        new DataColumn("IsActive", typeof(bool))
    });

            try
            {
                using var stream = new MemoryStream();
                await excelFile.CopyToAsync(stream);
                stream.Position = 0;

                using var workbook = new XLWorkbook(stream);

                // ------------------ 2) READ SHEET 1: LOOKUP NAMES ------------------
                var namesSheet = workbook.Worksheet(1);

                _logger.Information("Processing LookupNames sheet: {SheetName}", namesSheet.Name);

                var nameRows = namesSheet.RowsUsed().Skip(1);

                foreach (var row in nameRows)
                {
                    var lookupName = row.Cell(1).GetString()?.Trim();
                    var exists = row.Cell(2).GetValue<bool>();

                    if (string.IsNullOrWhiteSpace(lookupName))
                        continue;

                    dtNames.Rows.Add(lookupName, exists);
                }

                if (dtNames.Rows.Count == 0)
                {
                    throw new InvalidDataException("LookupNames sheet has no valid data.");
                }

                // ------------------ 3) READ SHEET 2: LOOKUP VALUES ------------------
                var valuesSheet = workbook.Worksheet(2);

                _logger.Information("Processing LookupValues sheet: {SheetName}", valuesSheet.Name);

                var valueRows = valuesSheet.RowsUsed().Skip(1);

                foreach (var row in valueRows)
                {
                    var lookupName = row.Cell(1).GetString()?.Trim();
                    var itemId = row.Cell(2).GetValue<int?>();
                    var itemName = row.Cell(3).GetString()?.Trim();
                    var isActive = row.Cell(4).GetValue<bool>();

                    if (string.IsNullOrWhiteSpace(lookupName) || string.IsNullOrWhiteSpace(itemName))
                        continue;

                    dtValues.Rows.Add(lookupName, itemId, itemName, isActive);
                }

                if (dtValues.Rows.Count == 0)
                {
                    throw new InvalidDataException("LookupValues sheet has no valid data.");
                }

                // ------------------ 4) CALL STORED PROCEDURE ------------------

                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("sp_UpsertLookupValuesFromExcelV2", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@TenantId", tenantId);
                cmd.Parameters.AddWithValue("@CreatedByUserId", userId);

                var namesParam = new SqlParameter("@LookupNames", SqlDbType.Structured)
                {
                    TypeName = "dbo.LookupNamesUploadType",
                    Value = dtNames
                };

                var valuesParam = new SqlParameter("@LookupValues", SqlDbType.Structured)
                {
                    TypeName = "dbo.LookupUploadTableType",
                    Value = dtValues
                };

                cmd.Parameters.Add(namesParam);
                cmd.Parameters.Add(valuesParam);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                _logger.Information("Lookup Excel V2 processed successfully for tenant {TenantId}", tenantId);

                return true;
            }
            catch (SqlException ex)
            {
                _logger.Error(ex, "SQL error while uploading lookup Excel V2.");

                // Pass clean SQL validation messages to UI
                throw ex;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unexpected error while processing lookup Excel upload.");
                throw;
            }
        }

        // CREATE LOOKUP
        public async Task CreateLookupNameAsync(LookupTableNameDto dto, string userId, int tenantId)
        {
            _logger.Information("Creating lookup name {LookupName} for TenantId={TenantId} by user {UserId}", dto.LookupName, dto.TenantId, userId);

            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var sql = @"
                    INSERT INTO dbo.tbl_CustomLookupTableNames
                        (TenantId, LookupName,IsCustom , CreatedByUserId, CreatedOn)
                    VALUES
                        (@TenantId, @LookupName,1, @CreatedByUserId, GETUTCDATE());";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@TenantId",  tenantId);
                cmd.Parameters.AddWithValue("@LookupName", dto.LookupName);
                cmd.Parameters.AddWithValue("@CreatedByUserId", userId);

                await cmd.ExecuteNonQueryAsync();
                _logger.Information("Lookup name {LookupName} created successfully by {UserId}", dto.LookupName, userId);
            }
            catch (SqlException ex)
            {
                _logger.Error(ex, "SQL error while creating lookup name {LookupName}. ErrorNumber={ErrorNumber}", dto.LookupName, ex.Number);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unexpected error while creating lookup name {LookupName}", dto.LookupName);
                throw;
            }
        }

        // READ ALL LOOKUPS
        public async Task<IEnumerable<LookupTableNameDto>> GetAllLookupNamesAsync(int tenantId)
        {
            _logger.Information("Fetching all lookup table names");

            var list = new List<LookupTableNameDto>();

            try
            {
                using var conn = new SqlConnection(_connectionString);
                
                await conn.OpenAsync();

                var sql = @"
                    SELECT Id, TenantId, LookupName, IsCustom
                    FROM dbo.tbl_CustomLookupTableNames where TenantId=@tenantId
                    ORDER BY LookupName ASC;";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@TenantId", tenantId);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(new LookupTableNameDto
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        TenantId = reader.IsDBNull(reader.GetOrdinal("TenantId")) ? null : reader.GetInt32(reader.GetOrdinal("TenantId")),
                        LookupName = reader.GetString(reader.GetOrdinal("LookupName")),
                        IsCustom = reader.GetBoolean(reader.GetOrdinal("IsCustom"))
                    });
                }

                _logger.Information("Fetched {Count} lookup names", list.Count);
                return list;
            }
            catch (SqlException ex)
            {
                _logger.Error(ex, "SQL error while fetching lookup names. ErrorNumber={ErrorNumber}", ex.Number);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unexpected error while fetching lookup names");
                throw;
            }
        }



        // UPDATE LOOKUP
        public async Task UpdateLookupNameAsync(LookupTableNameDto dto, string userId)
        {
            _logger.Information("Updating lookup name Id={Id} to {LookupName} by user {UserId}", dto.Id, dto.LookupName, userId);

            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var sql = @"
                    UPDATE dbo.tbl_CustomLookupTableNames
                    SET LookupName=@LookupName,
                        UpdatedByUserId=@UpdatedByUserId,
                        UpdatedOn=GETUTCDATE()
                    WHERE Id=@Id and TenantId=@TenantId and IsCustom=1;";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@LookupName", dto.LookupName);
                cmd.Parameters.AddWithValue("@UpdatedByUserId", userId);
                cmd.Parameters.AddWithValue("@Id", dto.Id);
                cmd.Parameters.AddWithValue("@TenantId", dto.TenantId);

                var rows = await cmd.ExecuteNonQueryAsync();

                if (rows == 0)
                    _logger.Warning("No record found for LookupName Id={Id} to update", dto.Id);
                else
                    _logger.Information("Lookup name Id={Id} updated successfully", dto.Id);
            }
            catch (SqlException ex)
            {
                _logger.Error(ex, "SQL error while updating lookup name Id={Id}. ErrorNumber={ErrorNumber}", dto.Id, ex.Number);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unexpected error while updating lookup name Id={Id}", dto.Id);
                throw;
            }
        }

        // DELETE LOOKUP
        public async Task DeleteLookupNameAsync(int id)
        {
            _logger.Information("Deleting lookup name Id={Id}", id);

            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var sql = "DELETE FROM dbo.tbl_CustomLookupTableNames WHERE Id=@Id and TenantId=@TenantId and IsCustom=0;";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                //cmd.Parameters.AddWithValue("@TenantId", TenantId);

                var rows = await cmd.ExecuteNonQueryAsync();

                if (rows == 0)
                    _logger.Warning("No lookup name found with Id={Id} to delete", id);
                else
                    _logger.Information("Lookup name Id={Id} deleted successfully", id);
            }
            catch (SqlException ex)
            {
                _logger.Error(ex, "SQL error while deleting lookup name Id={Id}. ErrorNumber={ErrorNumber}", id, ex.Number);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unexpected error while deleting lookup name Id={Id}", id);
                throw;
            }
        }


        // CREATE LOOKUP ITEM
        public async Task CreateLookupValueAsync(LookupTableValueDto dto, string userId, int tenantId)
        {
            _logger.Information("Creating lookup value {ItemName} for LookupId={LookupId} by user {UserId}",
                dto.ItemName, dto.LookupId, userId);

            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var sql = @"
                    INSERT INTO dbo.tbl_CustomLookupTableValues
                        (TenantId, LookupId, [Order], [Text], [IsActive], [IsCustom], [CreatedByUserId], [CreatedOn])
                    VALUES
                        (@TenantId, @LookupId, 
                         ISNULL((SELECT ISNULL(MAX([Order]),0)+1 FROM dbo.tbl_CustomLookupTableValues WHERE LookupId=@LookupId),1),
                         @Text, @IsActive, 1, @CreatedByUserId, GETUTCDATE());";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@TenantId", tenantId);
                cmd.Parameters.AddWithValue("@LookupId", dto.LookupId);
                cmd.Parameters.AddWithValue("@Text", dto.ItemName);
                cmd.Parameters.AddWithValue("@IsActive", dto.IsActive);
                cmd.Parameters.AddWithValue("@CreatedByUserId", userId);

                await cmd.ExecuteNonQueryAsync();
                _logger.Information("Lookup value {ItemName} created successfully for LookupId={LookupId}", dto.ItemName, dto.LookupId);
            }
            catch (SqlException ex)
            {
                _logger.Error(ex, "SQL error while creating lookup value {ItemName}. ErrorNumber={ErrorNumber}", dto.ItemName, ex.Number);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unexpected error while creating lookup value {ItemName}", dto.ItemName);
                throw;
            }
        }

        // READ ALL LOOKUP ITEMS 
        public async Task<IEnumerable<LookupTableValueDto>> GetAllLookupValuesAsync(int tenantId)
        {
            _logger.Information("Fetching all lookup values ");

            var list = new List<LookupTableValueDto>();

            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var sql = @"
                    SELECT Id, TenantId, LookupId, [Text] AS ItemName, [IsActive], IsCustom, [Order]  ,[CreatedOn],[UpdatedOn]
                    FROM dbo.tbl_CustomLookupTableValues where TenantId=@TenantId
                    ORDER BY [Order];";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@TenantId", tenantId);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new LookupTableValueDto
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        TenantId = reader.IsDBNull(reader.GetOrdinal("TenantId")) ? null : reader.GetInt32(reader.GetOrdinal("TenantId")),
                        LookupId = reader.GetInt32(reader.GetOrdinal("LookupId")),
                        ItemName = reader.GetString(reader.GetOrdinal("ItemName")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                        IsCustom = reader.GetBoolean(reader.GetOrdinal("IsCustom")),
                        Order = reader.IsDBNull(reader.GetOrdinal("Order")) ? null : reader.GetInt32(reader.GetOrdinal("Order")),
                        CreatedDate = reader.IsDBNull(reader.GetOrdinal("CreatedOn")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("CreatedOn")),
                        UpdatedDate = reader.IsDBNull(reader.GetOrdinal("UpdatedOn")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("UpdatedOn"))
                    });
                }

                _logger.Information("Fetched {Count} lookup values", list.Count);
                return list;
            }
            catch (SqlException ex)
            {
                _logger.Error(ex, "SQL error while fetching lookup values. ErrorNumber={ErrorNumber}",  ex.Number);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unexpected error while fetching lookup values");
                throw;
            }
        }

       

        // UPDATE LOOKUP VALUE
        public async Task UpdateLookupValueAsync(LookupTableValueDto dto, string userId)
        {
            _logger.Information("Updating lookup value Id={Id} by user {UserId}", dto.Id, userId);

            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var sql = @"
                    UPDATE dbo.tbl_CustomLookupTableValues
                    SET [Text]=@Text,
                        [IsActive]=@IsActive,
                        [Order]=@Order,
                        UpdatedByUserId=@UpdatedByUserId,
                        UpdatedOn=GETUTCDATE()
                    WHERE Id=@Id AND TenantId=@TenantId AND LookupId=@LookupId and IsCustom=1;";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Text", dto.ItemName);
                cmd.Parameters.AddWithValue("@IsActive", dto.IsActive);
                cmd.Parameters.AddWithValue("@Order", dto.Order ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@UpdatedByUserId", userId);
                cmd.Parameters.AddWithValue("@Id", dto.Id);
                cmd.Parameters.AddWithValue("@TenantId", dto.TenantId);
                cmd.Parameters.AddWithValue("@LookupId", dto.LookupId);

                var rows = await cmd.ExecuteNonQueryAsync();
                _logger.Information("Lookup value Id={Id} updated ({RowsAffected} rows).", dto.Id, rows);
            }
            catch (SqlException ex)
            {
                _logger.Error(ex, "SQL error while updating lookup value Id={Id}. ErrorNumber={ErrorNumber}", dto.Id, ex.Number);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unexpected error while updating lookup value Id={Id}", dto.Id);
                throw;
            }
        }

        // DELETE LOOKUP VALUE
        public async Task DeleteLookupValueAsync(int id)
        {
            _logger.Information("Deleting lookup value Id={Id}", id);

            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var sql = "DELETE FROM dbo.tbl_CustomLookupTableValues WHERE Id=@Id AND TenantId=@TenantId AND LookupId=@LookupId and IsCustom=0;";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                //cmd.Parameters.AddWithValue("@TenantId", dto.TenantId);
                //cmd.Parameters.AddWithValue("@LookupId", dto.LookupId);

                var rows = await cmd.ExecuteNonQueryAsync();
                _logger.Information("Lookup value Id={Id} deleted ({RowsAffected} rows).", id, rows);
            }
            catch (SqlException ex)
            {
                _logger.Error(ex, "SQL error while deleting lookup value Id={Id}. ErrorNumber={ErrorNumber}", id, ex.Number);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unexpected error while deleting lookup value Id={Id}", id);
                throw;
            }
        }

        // READ LOOKUP VALUES BY LOOKUP ID
        public async Task<List<LookupTableValueDto>> GetLookupValuesByLookupIdAsync(int lookupId, int tenantId)
        {
            _logger.Information("Fetching lookup values for LookupId={LookupId}, TenantId={TenantId}", lookupId, tenantId);

            var list = new List<LookupTableValueDto>();

            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var sql = @"
            SELECT 
                Id,
                TenantId,
                LookupId,
                [Text] AS ItemName,
                IsActive,
                IsCustom,
                [Order],
                CreatedOn,
                UpdatedOn
            FROM dbo.tbl_CustomLookupTableValues
            WHERE LookupId = @LookupId
              AND TenantId = @TenantId
              AND IsActive = 1
            ORDER BY [Order];";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@LookupId", lookupId);
                cmd.Parameters.AddWithValue("@TenantId", tenantId);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new LookupTableValueDto
                    {
                        Id = reader.IsDBNull(reader.GetOrdinal("Id"))
             ? null
             : reader.GetInt32(reader.GetOrdinal("Id")),

                        TenantId = reader.IsDBNull(reader.GetOrdinal("TenantId"))
             ? null
             : reader.GetInt32(reader.GetOrdinal("TenantId")),

                        LookupId = reader.GetInt32(reader.GetOrdinal("LookupId")),
                        ItemName = reader.GetString(reader.GetOrdinal("ItemName")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                        IsCustom = reader.GetBoolean(reader.GetOrdinal("IsCustom")),

                        Order = reader.IsDBNull(reader.GetOrdinal("Order"))
             ? null
             : reader.GetInt32(reader.GetOrdinal("Order")),

                        CreatedDate = reader.IsDBNull(reader.GetOrdinal("CreatedOn"))
             ? null
             : reader.GetDateTime(reader.GetOrdinal("CreatedOn")),

                        UpdatedDate = reader.IsDBNull(reader.GetOrdinal("UpdatedOn"))
             ? null
             : reader.GetDateTime(reader.GetOrdinal("UpdatedOn"))
                    });

                }

                _logger.Information("Fetched {Count} lookup values for LookupId={LookupId}", list.Count, lookupId);
                return list;
            }
            catch (SqlException ex)
            {
                _logger.Error(ex, "SQL error while fetching lookup values for LookupId={LookupId}", lookupId);
                throw;
            }
        }

    }
}
