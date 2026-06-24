using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using ClosedXML.Excel;
using Microsoft.Data.SqlClient;
using Serilog;
using System.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BRaVe_Management_Backend.Services
{
    public class SqlLocationService : ILocationService
    {
        private string connectionString { get; set; }
        private readonly ILogger<SqlLocationService> _logger;
        public SqlLocationService(ISecretProvider secretProvider, ILogger<SqlLocationService> logger) 
        {
            _logger = logger;
            connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection).Result;
        }

        public async Task CreateLocation(LocationDto data, string userId)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("sp_CreateLocation", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@OfficialCode", SqlDbType.VarChar, 100)
                    .Value = data.OfficialCode;

                cmd.Parameters.Add("@LocationName", SqlDbType.VarChar, 100)
                    .Value = data.LocationName;

                cmd.Parameters.Add("@IsActive", SqlDbType.Bit)
                    .Value = data.IsActive;

                cmd.Parameters.Add("@LevelId", SqlDbType.Int)
                    .Value = data.LevelId;

                cmd.Parameters.Add("@TenantId", SqlDbType.Int)
                    .Value = data.TenantId;

                cmd.Parameters.Add("@CreatedByUserId", SqlDbType.VarChar, 50)
                    .Value = userId;

                cmd.Parameters.Add("@ParentLocationId", SqlDbType.Int)
                    .Value = (object?)data.ParentLocationId ?? DBNull.Value;

                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                
                throw new Exception($"CreateLocation failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw;
            }
        }



        public async Task UpdateLocation(Location data, string UserId)
        {
            
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var cmd = new SqlCommand(@"UPDATE lkp_Locations SET  LocationName=@LocationName,IsActive=@IsActive,
                OfficialCode=@OfficialCode,ParentLocationId=@ParentLocationId,LevelId=@LevelId, UpdatedByUserId=@UpdatedByUserId, UpdatedOn=GETUTCDATE() WHERE Id=@Id and TenantId=@TenantId", conn);
                cmd.Parameters.AddWithValue("@LocationName", data.LocationName);
                 cmd.Parameters.AddWithValue("@IsActive", data.IsActive);
                cmd.Parameters.AddWithValue("@OfficialCode", data.OfficialCode);
                cmd.Parameters.Add(
                    new SqlParameter("@ParentLocationId", SqlDbType.Int)
                    {
                        Value = (object?)data.ParentLocationId ?? DBNull.Value
                    }
                );

                cmd.Parameters.AddWithValue("@LevelId", data.LevelId);
                cmd.Parameters.AddWithValue("@Id", data.Id);
                cmd.Parameters.AddWithValue("@TenantId", data.TenantId);

                cmd.Parameters.AddWithValue("@UpdatedByUserId", UserId);
                await cmd.ExecuteNonQueryAsync();
            }

            catch (Exception ex)
            {
              throw;
            }
        }

        
        public async Task<IEnumerable<Location>> GetAllLocations(int TenantId)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                                        var sql =
                            "SELECT c.Id, c.TenantId, c.LocationName, c.CreatedOn, " +
                            "c.OfficialCode, c.IsActive, c.ParentLocationId, " +
                            "p.LocationName AS ParentLocationName, " +
                            "c.CreatedByUserId, c.UpdatedOn, c.LevelId " +
                            "FROM lkp_Locations c " +
                            "LEFT JOIN lkp_Locations p " +
                            "ON c.ParentLocationId = p.Id " +
                            "AND c.TenantId = p.TenantId " +
                            "WHERE c.TenantId = @TenantId";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@TenantId", TenantId);

                List<Location> Locations = new List<Location>();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var location = new Location()
                        {

                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            LocationName = reader.GetString(reader.GetOrdinal("LocationName")),
                            ParentLocationId = reader.IsDBNull(reader.GetOrdinal("ParentLocationId")) ? null : reader.GetInt32(reader.GetOrdinal("ParentLocationId")),
                            ParentLocationName = reader.IsDBNull(reader.GetOrdinal("ParentLocationName")) ? null : reader.GetString(reader.GetOrdinal("ParentLocationName")),
                            LevelId = reader.GetInt32(reader.GetOrdinal("LevelId")),
                            CreatedOn = reader.GetDateTime(reader.GetOrdinal("CreatedOn")),
                            OfficialCode = reader.IsDBNull(reader.GetOrdinal("OfficialCode")) ? null : reader.GetString(reader.GetOrdinal("OfficialCode")),
                            UpdatedOn = reader.IsDBNull(reader.GetOrdinal("UpdatedOn")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedOn")),
                            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                        };

                        if (!location.UpdatedOn.HasValue)
                            location.UpdatedOn = location.CreatedOn;

                        Locations.Add(location);

                    }
                }

                return Locations;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error fetching all administrative levels for Tenant={TenantId}");

                throw e;
            }
        }

        public async Task<IEnumerable<Location>> GetAllLocationbytenantId(int tenantId)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = "SELECT Id,TenantId,LocationName,CreatedOn,OfficialCode,IsActive,ParentLocationId ,CreatedByUserId,IsActive,UpdatedOn,LevelId from lkp_Locations where TenantId=@TenantId";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@tenantId", tenantId);

                List<Location> Locations = new List<Location>();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var location = new Location()
                        {

                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            LocationName = reader.GetString(reader.GetOrdinal("LocationName")),
                            ParentLocationId = reader.GetInt32(reader.GetOrdinal("ParentLocationId")),
                            LevelId = reader.GetInt32(reader.GetOrdinal("LevelId")),

                            CreatedOn = reader.GetDateTime(reader.GetOrdinal("CreatedOn")),
                            OfficialCode = reader.GetString(reader.GetOrdinal("OfficialCode")),

                            UpdatedOn = reader.GetDateTime(reader.GetOrdinal("UpdatedOn")),

                            // EnumeratorPin = reader.IsDBNull(reader.GetOrdinal("EnumeratorPin")) ? null : reader.GetString(reader.GetOrdinal("FullName")),
                            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                        };
                        Locations.Add(location);

                    }
                }

                return Locations;
            }
            catch (Exception e)
            {
                return null;
            }
        }


        /// Uploads an Excel file containing location values and updates the database via a stored procedure.
        public async Task<string> UploadAdministrativeLevelLocationExcelAsync(
        IFormFile excelFile, string userId, int tenantId)
        {
            if (excelFile == null || excelFile.Length == 0)
                throw new ArgumentException("No file uploaded.");

            var dt = new DataTable();

            // ✅ MUST MATCH TVP EXACTLY (ORDER + COUNT)
            dt.Columns.Add("RowNumber", typeof(int));
            dt.Columns.Add("ID", typeof(int));
            dt.Columns.Add("Name", typeof(string));
            dt.Columns.Add("Level", typeof(int));
            dt.Columns.Add("Parent", typeof(int));
            dt.Columns.Add("OfficialCode", typeof(string));
            dt.Columns.Add("IsActive", typeof(bool));

            try
            {
                using var stream = new MemoryStream();
                await excelFile.CopyToAsync(stream);
                stream.Position = 0;

                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheets.First();

                var rows = worksheet.RowsUsed().Skip(1);

                foreach (var row in rows)
                {
                    var rowNumber = row.RowNumber(); // ✅ REQUIRED

                    var ID = row.Cell(1).GetValue<int?>();
                    var Name = row.Cell(2).GetString()?.Trim();
                    var Level = row.Cell(3).GetValue<int?>();
                    var Parent = row.Cell(4).GetValue<int?>();
                    var OfficialCode = row.Cell(5).GetString()?.Trim();
                    var IsActive = row.Cell(6).GetValue<bool>();

                    if (string.IsNullOrWhiteSpace(Name) || Level == null)
                        continue;

                    dt.Rows.Add(
                        rowNumber, // 1️⃣ MUST MATCH TVP
                        (object?)ID ?? DBNull.Value,
                        Name,
                        Level,
                        (object?)Parent ?? DBNull.Value,
                        (object?)OfficialCode ?? DBNull.Value,
                        IsActive
                    );
                }

                if (dt.Rows.Count == 0)
                    return "No valid rows found in the template.";

                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("sp_UpsertLocationsFromExcel", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                var tvpParam = cmd.Parameters.AddWithValue("@Locations", dt);
                tvpParam.SqlDbType = SqlDbType.Structured;
                tvpParam.TypeName = "dbo.LocationExcelType";

                cmd.Parameters.AddWithValue("@TenantId", tenantId);
                cmd.Parameters.AddWithValue("@UserId", userId);

                using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    int total = reader.GetInt32(reader.GetOrdinal("TotalProcessed"));
                    int inserted = reader.GetInt32(reader.GetOrdinal("Inserted"));
                    int updated = reader.GetInt32(reader.GetOrdinal("Updated"));

                    return $"Upload complete. {total} record(s) processed ({inserted} inserted, {updated} updated).";
                }

                return "Upload complete.";
            }
            catch (SqlException ex)
            {
                throw new ApplicationException(ex.Message);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Unexpected error during upload.", ex);
            }
        }


        public async Task<byte[]> DownloadAdministrativeLevelLocationTemplateAsync(int tenantId)
        {
            _logger.LogInformation("Generating Administrative Level Location template for tenant {TenantId}", tenantId);

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("sp_GetAdministrativeLevelLocationTemplate", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@TenantId", tenantId);

                var dt = new DataTable();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    dt.Load(reader);
                }

                // Create Excel
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Locations Template");

                // Add headers
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    worksheet.Cell(1, i + 1).Value = dt.Columns[i].ColumnName;
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                }

                // Add data
                for (int row = 0; row < dt.Rows.Count; row++)
                {
                    for (int col = 0; col < dt.Columns.Count; col++)
                    {
                        var value = dt.Rows[row][col];

                        worksheet.Cell(row + 2, col + 1).Value = value switch
                        {
                            DBNull => "",
                            int i => i,
                            bool b => b,
                            DateTime d => d,
                            _ => value.ToString()
                        };
                    }
                }

                worksheet.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);

                _logger.LogInformation("Template generated successfully for tenant {TenantId}", tenantId);

                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Administrative Level Location template.");
                throw;
            }
        }








        public Task<Location?> GetLocationById(int id)
        {
            throw new NotImplementedException();
        }
    }
}
