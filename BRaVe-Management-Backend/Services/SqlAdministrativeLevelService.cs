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
    public class SqlAdministrativeLevelService : IAdministrativeLevelService
    {
        private string connectionString { get; set; }
        private readonly string _connectionString;
        private readonly ILogger<SqlAdministrativeLevelService> _logger;
        public SqlAdministrativeLevelService(ISecretProvider secretProvider, ILogger<SqlAdministrativeLevelService> logger) {

            _logger = logger;
            connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection).Result;
                
                //config.GetConnectionString("DefaultConnection");
        }
 
        public async Task CreateAdministrativeLevel(AdministrativeLevelDto data, string UserId)
        {

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();
              
                var cmd = new SqlCommand(@"sp_CreateAdministrativeLevel @OfficialCode,@LevelName,@IsActive,@Id,@TenantId, @CreatedByUserId", conn);
                cmd.Parameters.AddWithValue("@OfficialCode", data.OfficialCode);
                cmd.Parameters.AddWithValue("@LevelName", data.LevelName);
                cmd.Parameters.AddWithValue("@Id", data.Id);
                cmd.Parameters.AddWithValue("@TenantId", data.TenantId);

                cmd.Parameters.AddWithValue("@IsActive", data.IsActive);

                 cmd.Parameters.AddWithValue("@CreatedByUserId", UserId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {

                throw;
            }
           
        }


        public async Task UpdateAdministrativeLevel(AdministrativeLevel data, string UserId)
        {

            try
            {

                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var cmd = new SqlCommand(@"UPDATE lkp_AdministrativeLevels SET  LevelName=@LevelName,IsActive=@IsActive,OfficialCode=@OfficialCode, UpdatedByUserId=@UpdatedByUserId, UpdatedOn=GETUTCDATE() WHERE Id=@Id", conn);
                cmd.Parameters.AddWithValue("@LevelName", data.LevelName);
                 cmd.Parameters.AddWithValue("@IsActive", data.IsActive);
                cmd.Parameters.AddWithValue("@OfficialCode", data.OfficialCode);
                cmd.Parameters.AddWithValue("@Id", data.Id);

                cmd.Parameters.AddWithValue("@UpdatedByUserId", UserId);
                await cmd.ExecuteNonQueryAsync();
            }

            catch (Exception ex)
            {
              var  Error1 = ex;
            }
        }

        
        public async Task<IEnumerable<AdministrativeLevel>> GetAllAdministrativeLevel(int TenantId)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = "SELECT Id,TenantId,LevelName,CreatedOn,OfficialCode,IsActive ,CreatedByUserId,TenantId,IsActive,UpdatedOn from lkp_AdministrativeLevels WHERE TenantId=@TenantId";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@TenantId", TenantId);

                List<AdministrativeLevel> AdministrativeLevels = new List<AdministrativeLevel>();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var administrativeLevel = new AdministrativeLevel()
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            LevelName = reader.GetString(reader.GetOrdinal("LevelName")),
                            CreatedByUserId = reader.GetString(reader.GetOrdinal("CreatedByUserId")),
                            CreatedOn = reader.GetDateTime(reader.GetOrdinal("CreatedOn")),
                             OfficialCode = reader.IsDBNull(reader.GetOrdinal("OfficialCode")) ? null : reader.GetString(reader.GetOrdinal("OfficialCode")),
                            UpdatedOn = reader.IsDBNull(reader.GetOrdinal("UpdatedOn")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedOn")),
                            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                         };

                        if (!administrativeLevel.UpdatedOn.HasValue)
                            administrativeLevel.UpdatedOn = administrativeLevel.CreatedOn;

                        AdministrativeLevels.Add(administrativeLevel);

                    }
                }

                return AdministrativeLevels;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error fetching all administrative levels for Tenant={TenantId}");

                throw e;
            }
        }






        /// Uploads an Excel file containing lookup values and updates the database via a stored procedure.
        public async Task<bool> UploadAdministrativeLevelExcelAsync(IFormFile excelFile, string userId, int tenantId)
        {
            _logger.LogInformation("Starting Administrative Level item type Excel upload for user {UserId}, tenant {TenantId}", userId, tenantId);

            if (excelFile == null || excelFile.Length == 0)
            {
                _logger.LogError("No Excel file provided.");
                throw new ArgumentException("No file uploaded.");
            }

            // 1️ Read Excel into DataTable
            var dt = new DataTable();
            dt.Columns.AddRange(new[]
            {

                new DataColumn("ID", typeof(int)),
                new DataColumn("Name", typeof(string)),
                    new DataColumn("OfficialCode", typeof(string)),
                new DataColumn("IsActive", typeof(bool))
            });

            try
            {
                using var stream = new MemoryStream();
                await excelFile.CopyToAsync(stream);
                stream.Position = 0;

                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheets.First();

                _logger.LogInformation("Processing worksheet: {WorksheetName}", worksheet.Name);

                var rows = worksheet.RowsUsed().Skip(1); // Skip header

                foreach (var row in rows)
                {

                    var ID = row.Cell(1).GetValue<int?>();
                    var Name = row.Cell(2).GetString()?.Trim();
                    var OfficialCode = row.Cell(3).GetString()?.Trim();
                    var isActive = row.Cell(4).GetValue<bool>();

                    if (string.IsNullOrWhiteSpace(OfficialCode) || string.IsNullOrWhiteSpace(Name))
                        continue; // Skip incomplete rows

                    dt.Rows.Add(ID, Name, OfficialCode, isActive);
                    //saving to database
                    try
                    {
                        using var connn = new SqlConnection(connectionString);
                        await connn.OpenAsync();

                        var cmdd = new SqlCommand(@"sp_CreateAdministrativeLevelExcel @ID,@Name,@OfficialCode,@isActive,@TenantId,@CreatedByUserId", connn);


                        cmdd.Parameters.AddWithValue("@ID", ID);

                        cmdd.Parameters.AddWithValue("@Name", Name);
                        cmdd.Parameters.AddWithValue("@OfficialCode", OfficialCode);
                        cmdd.Parameters.AddWithValue("@isActive", isActive);
                        cmdd.Parameters.AddWithValue("@TenantId", tenantId);
                        cmdd.Parameters.AddWithValue("@CreatedByUserId", userId);
                        await cmdd.ExecuteNonQueryAsync();
                        _logger.LogInformation("Administrative Level Excel processed successfully for tenant {TenantId}", tenantId);
                        //return true;
                    }

                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "SQL error while uploading Administrative Level Excel.");

                        // Try to detect missing lookup names message
                        if (ex.Message.Contains("Missing Name", StringComparison.OrdinalIgnoreCase))
                        {
                            // Extract the detailed message
                            throw new ApplicationException(ex.Message);
                        }

                        // Generic SQL error fallback
                        throw new ApplicationException("Database error while uploading Administrative Level Excel.", ex);
                    }
                    //ende



                }
                return true;

            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while processing Administrative Level Excel upload.");
                throw;
            }
        }



        public Task<AdministrativeLevel?> GetAdministrativeLevelById(int id)
        {
            throw new NotImplementedException();
        }

     
        /// Uploads an Excel file containing lookup values and updates the database via a stored procedure.
        public async Task<bool> UploadAdministrativeLevelLocationExcelAsync(IFormFile excelFile, string userId, int tenantId)
        {
            _logger.LogInformation("Starting Administrative Level Location item type Excel upload for user {UserId}, tenant {TenantId}", userId, tenantId);

            if (excelFile == null || excelFile.Length == 0)
            {
                _logger.LogWarning("No Excel file provided.");
                throw new ArgumentException("No file uploaded.");
            }

            // 1️ Read Excel into DataTable
            var dt = new DataTable();
            dt.Columns.AddRange(new[]
            {

                new DataColumn("ID", typeof(int)),
                new DataColumn("Name", typeof(string)),
                    new DataColumn("Level", typeof(string)),
                            new DataColumn("Parent", typeof(string)),
                                          new DataColumn("OfficialCode", typeof(string)),
                new DataColumn("IsActive", typeof(bool))
            });

            try
            {
                using var stream = new MemoryStream();
                await excelFile.CopyToAsync(stream);
                stream.Position = 0;

                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheets.First();

                _logger.LogInformation("Processing worksheet: {WorksheetName}", worksheet.Name);

                var rows = worksheet.RowsUsed().Skip(1); // Skip header

                foreach (var row in rows)
                {

                    var ID = row.Cell(1).GetValue<int?>();
                    var Name = row.Cell(2).GetString()?.Trim();
                    var Level = row.Cell(3).GetString()?.Trim();
                    var Parent = row.Cell(4).GetString()?.Trim();
                    var OfficialCode = row.Cell(5).GetString()?.Trim();
                    var isActive = 1;// row.Cell(6).GetString()?.Trim();

                    //var isActive1 = row.Cell(6).GetValue<bool>();
                    //var isActive = Convert.ToBoolean(isActive1);
                    if (string.IsNullOrWhiteSpace(Level) || string.IsNullOrWhiteSpace(Name))
                        continue; // Skip incomplete rows

                    dt.Rows.Add(ID, Name, Level, Parent, OfficialCode);
                    //saving to database
                    try
                    {
                        using var connn = new SqlConnection(connectionString);
                        await connn.OpenAsync();

                        var cmdd = new SqlCommand(@"sp_CreateAdministrativeLevelLocationExcel @ID,@Name,@Level,@Parent,@OfficialCode,@checkisActive,@TenantId,@CreatedByUserId", connn);


                        cmdd.Parameters.AddWithValue("@ID", ID);

                        cmdd.Parameters.AddWithValue("@Name", Name);
                        cmdd.Parameters.AddWithValue("@Level", Level);
                        cmdd.Parameters.AddWithValue("@Parent", Parent);
                        cmdd.Parameters.AddWithValue("@OfficialCode", OfficialCode);
                        cmdd.Parameters.AddWithValue("@checkisActive", isActive);
                        cmdd.Parameters.AddWithValue("@TenantId", tenantId);
                        cmdd.Parameters.AddWithValue("@CreatedByUserId", userId);
                        await cmdd.ExecuteNonQueryAsync();
                        _logger.LogInformation("Administrative Level Location Excel processed successfully for tenant {TenantId}", tenantId);
                        //return true;
                    }

                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "SQL error while uploading Administrative Level Location Excel.");

                        // Try to detect missing lookup names message
                        if (ex.Message.Contains("Missing Name", StringComparison.OrdinalIgnoreCase))
                        {
                            // Extract the detailed message
                            throw new ApplicationException(ex.Message);
                        }

                        // Generic SQL error fallback
                        throw new ApplicationException("Database error while uploading Administrative Level Location Excel.", ex);
                    }
                    //ende



                }
                return true;

            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while processing Administrative Level Location Excel upload.");
                throw;
            }
        }


    }
}
