using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BRaVe_Management_Backend.Services
{
    public class SqlEnumeratorService : IEnumeratorService
    {
        private string connectionString { get; set; }
        public SqlEnumeratorService(ISecretProvider secretProvider) {

            connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection).Result;
                
                //config.GetConnectionString("DefaultConnection");
        }
 
        public async Task CreateEnumerator(EnumeratorDto data, string UserId)
        {

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();
              
                var cmd = new SqlCommand(@"sp_CreateEnumerator @EnumeratorCode,@FullName,@IsSupervisor, @Note, @CreatedByUserId", conn);
                cmd.Parameters.AddWithValue("@EnumeratorCode", data.EnumeratorCode);
                cmd.Parameters.AddWithValue("@FullName", data.FullName);
                //cmd.Parameters.AddWithValue("@EnumeratorType", data.EnumeratorType);
                //cmd.Parameters.AddWithValue("@EnumeratorPin", data.EnumeratorPin);

                //cmd.Parameters.AddWithValue("@IsActive", data.IsActive);
                cmd.Parameters.AddWithValue("@IsSupervisor", data.IsSupervisor);

                cmd.Parameters.AddWithValue("@Note", data.Note);
                cmd.Parameters.AddWithValue("@CreatedByUserId", UserId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                string error=ex.Message;
            }
           
        }
        

        public async Task UpdateEnumerator(Enumerator data, string UserId)
        {

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                string Type = "E";

                if (data.IsSupervisor) {
                    Type = "S";
                }

                //EnumeratorType

                var cmd = new SqlCommand(@"UPDATE tbl_Enumerators SET  FullName=@FullName,IsActive=@IsActive,IsPinUpdated=@IsPinUpdated, EnumeratorCode=@EnumeratorCode, EnumeratorType=@Type, Note=@Note, UpdatedByUserId=@UpdatedByUserId, UpdatedOn=GETUTCDATE() WHERE EnumeratorId=@EnumeratorId", conn);
                cmd.Parameters.AddWithValue("@FullName", data.FullName);
                cmd.Parameters.AddWithValue("@IsActive", data.IsActive);
                cmd.Parameters.AddWithValue("@IsPinUpdated", data.IsPinUpdated);
                cmd.Parameters.AddWithValue("@EnumeratorCode", data.EnumeratorCode);
                cmd.Parameters.AddWithValue("@Type", Type);
                //cmd.Parameters.AddWithValue("@IsSupervisor", data.IsSupervisor);
                cmd.Parameters.AddWithValue("@Note", data.Note);
                cmd.Parameters.AddWithValue("@EnumeratorId", data.EnumeratorId);
                cmd.Parameters.AddWithValue("@UpdatedByUserId", UserId);
                await cmd.ExecuteNonQueryAsync();
            }

            catch (Exception ex)
            {
              var  Error1 = ex;
            }
        }

        public async Task<IEnumerable<Enumerator>> GetAllEnumeratorCode(int TenantId)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = "SELECT EnumeratorId,EnumeratorCode,EnumeratorPin,CreatedOn,FullName,Note ,CreatedByUserId,EnumeratorType,IsActive,IsPinUpdated,UpdatedOn from tbl_Enumerators where UpdatedByUserId is  null and TenantId=@TenantId";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@TenantId", TenantId);

                List<Enumerator> Enumerators = new List<Enumerator>();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var enumerator = new Enumerator()
                        {

                            EnumeratorId = reader.GetInt32(reader.GetOrdinal("EnumeratorId")),
                            EnumeratorCode = reader.GetString(reader.GetOrdinal("EnumeratorCode")),
                            CreatedByUserId = reader.GetString(reader.GetOrdinal("CreatedByUserId")),
                            CreatedOn = reader.GetDateTime(reader.GetOrdinal("CreatedOn")),
                            FullName = reader.IsDBNull(reader.GetOrdinal("FullName")) ? null : reader.GetString(reader.GetOrdinal("FullName")),
                            
                            Note = reader.IsDBNull(reader.GetOrdinal("Note")) ? null : reader.GetString(reader.GetOrdinal("Note")),
                            IsPinUpdated = reader.GetBoolean(reader.GetOrdinal("IsPinUpdated")),
                            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                            EnumeratorType = reader.GetString(reader.GetOrdinal("EnumeratorType"))
                        };

                        if (!enumerator.UpdatedOn.HasValue)
                            enumerator.UpdatedOn = enumerator.CreatedOn;

                        Enumerators.Add(enumerator);

                    }
                }

                return Enumerators;
            }
            catch (Exception e)
            {
                return null;
            }
        }
        public async Task<IEnumerable<Enumerator>> GetAllEnumerators(int TenantId)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = "SELECT EnumeratorId,EnumeratorCode,EnumeratorPin,CreatedOn,FullName,Note ,CreatedByUserId,EnumeratorType,IsActive,IsPinUpdated,UpdatedOn from tbl_Enumerators where UpdatedByUserId is not null and TenantId=@TenantId order by UpdatedOn desc";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@TenantId", TenantId);

                List<Enumerator> Enumerators = new List<Enumerator>();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var enumerator = new Enumerator()
                        {

                            EnumeratorId = reader.GetInt32(reader.GetOrdinal("EnumeratorId")),
                            EnumeratorCode = reader.GetString(reader.GetOrdinal("EnumeratorCode")),
                            CreatedByUserId = reader.GetString(reader.GetOrdinal("CreatedByUserId")),
                            CreatedOn = reader.GetDateTime(reader.GetOrdinal("CreatedOn")),
                            UpdatedOn = reader.GetDateTime(reader.GetOrdinal("UpdatedOn")),
                             FullName = reader.IsDBNull(reader.GetOrdinal("FullName")) ? null : reader.GetString(reader.GetOrdinal("FullName")),
                            Note = reader.IsDBNull(reader.GetOrdinal("Note"))?null:reader.GetString(reader.GetOrdinal("Note")),
                            IsPinUpdated = reader.GetBoolean(reader.GetOrdinal("IsPinUpdated")),
                             IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                            EnumeratorType = reader.GetString(reader.GetOrdinal("EnumeratorType"))
                        };

                        if (!enumerator.UpdatedOn.HasValue)
                            enumerator.UpdatedOn = enumerator.CreatedOn;

                        Enumerators.Add(enumerator);

                    }
                }

                return Enumerators;
            }
            catch (Exception e)
            {
                return null;
            }
        }
        public async Task<IEnumerable<EnumeratorCodeBatch>> GetAllCodeBatches(int TenantId)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = "SELECT TenantId,Prefix,SuffixLength,TotalCodeGenerated,LastCodeGenerated,LatestId ,CreatedByUserId,CreatedOn,UpdatedByUserId,UpdatedOn from tbl_EnumeratorIdGeneratorTracking where TenantId=@TenantId";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@TenantId", TenantId);

                List<EnumeratorCodeBatch> EnumeratorCodeBatchs = new List<EnumeratorCodeBatch>();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var enumeratorCodeBatch = new EnumeratorCodeBatch()
                        {

                            TenantId = reader.GetInt32(reader.GetOrdinal("TenantId")),
                            Prefix = reader.GetString(reader.GetOrdinal("Prefix")),
                            CreatedByUserId = reader.GetString(reader.GetOrdinal("CreatedByUserId")),
                            CreatedOn = reader.GetDateTime(reader.GetOrdinal("CreatedOn")),
                            SuffixLength = reader.GetInt32(reader.GetOrdinal("SuffixLength")),

                            TotalCodeGenerated = reader.GetInt32(reader.GetOrdinal("TotalCodeGenerated")),
                            LastCodeGenerated = reader.GetString(reader.GetOrdinal("LastCodeGenerated")),
                            LatestId = reader.GetInt32(reader.GetOrdinal("LatestId")),
                            UpdatedByUserId = reader.GetString(reader.GetOrdinal("UpdatedByUserId"))
                        };
                        EnumeratorCodeBatchs.Add(enumeratorCodeBatch);

                    }
                }

                return EnumeratorCodeBatchs;
            }
            catch (Exception e)
            {
                return null;
            }
        }
        //Task<IEnumerable<EnumeratorCodeBatch>> IEnumeratorService.GetAllCodeBatches()
        //{
        //    throw new NotImplementedException();
        //}


        //Task<IEnumerable<LookupItemDto>> IEnumeratorService.GetAvailableEnumeratorCodes()
        //{
        //    throw new NotImplementedException();
        //}

        public async Task<Enumerator?> GetEnumeratorById(int id)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = @"SELECT EnumeratorId,
                           EnumeratorCode,
                           EnumeratorPin,
                           CreatedOn,
                           FullName,
                           Note,
                           CreatedByUserId,
                           TenantId,
                           EnumeratorType,
                           IsActive,
                           IsPinUpdated,
                           UpdatedOn
                    FROM tbl_Enumerators
                    WHERE EnumeratorId = @EnumeratorId";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@EnumeratorId", id);

                using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow);

                if (await reader.ReadAsync())
                {
                    return new Enumerator
                    {
                        EnumeratorId = reader.GetInt32(reader.GetOrdinal("EnumeratorId")),
                        EnumeratorCode = reader.GetString(reader.GetOrdinal("EnumeratorCode")),
                        CreatedByUserId = reader.GetString(reader.GetOrdinal("CreatedByUserId")),
                        CreatedOn = reader.GetDateTime(reader.GetOrdinal("CreatedOn")),
                        FullName = reader.IsDBNull(reader.GetOrdinal("FullName")) ? null : reader.GetString(reader.GetOrdinal("FullName")),
                        Note = reader.IsDBNull(reader.GetOrdinal("Note")) ? null : reader.GetString(reader.GetOrdinal("Note")),
                        IsPinUpdated = reader.GetBoolean(reader.GetOrdinal("IsPinUpdated")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                        EnumeratorType = reader.GetString(reader.GetOrdinal("EnumeratorType"))
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching enumerator by Id: {ex.Message}");
                return null;
            }
        }

        public async Task CreateEnumeratorCodeBatch(EnumeratorCodeBatchDto data, string UserId)
         {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();
                //var TenantId = 1;
                var cmd = new SqlCommand(@"sp_GenerateEnumeratorCodes  @TenantId,@Prefix,@SuffixPaddingLength,@Total,@CreatedByUserId", conn);
                cmd.Parameters.AddWithValue("@TenantId", data.TenantId);
                cmd.Parameters.AddWithValue("@Prefix", data.Prefix);
                //cmd.Parameters.AddWithValue("@EnumeratorType", data.EnumeratorType);
                cmd.Parameters.AddWithValue("@SuffixPaddingLength", data.SuffixLength);

                cmd.Parameters.AddWithValue("@Total", data.TotalCodeGenerated);
     
                cmd.Parameters.AddWithValue("@CreatedByUserId", UserId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
        }

        public async Task ResetEnumeratorPin(int EnumeratorId, string UserId)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var cmd = new SqlCommand(@"
             UPDATE tbl_Enumerators
            SET 
            IsActive = @IsActive,
            EnumeratorPin = CONVERT(VARBINARY(MAX), NULL),
            IsPinUpdated = @IsPinUpdated,
            UpdatedByUserId = @UpdatedByUserId,
            UpdatedOn = GETUTCDATE()
             WHERE EnumeratorId = @EnumeratorId", conn);

                    cmd.Parameters.AddWithValue("@IsActive", true);
                cmd.Parameters.AddWithValue("@EnumeratorPin", DBNull.Value);
                cmd.Parameters.AddWithValue("@IsPinUpdated", false);
                cmd.Parameters.AddWithValue("@EnumeratorId", EnumeratorId);
                cmd.Parameters.AddWithValue("@UpdatedByUserId", UserId);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                var Error1 = ex;
                throw; // optional but recommended
            }
        }

        //async Task IEnumeratorService.UpdateEnumerator(EnumeratorDto data, string UserId)
        //{
        //    try
        //    {
        //        using var conn = new SqlConnection(connectionString);
        //        await conn.OpenAsync();

        //        var cmd = new SqlCommand(@"sp_CreateEnumerator @EnumeratorCode,@FullName,@EnumeratorType,@EnumeratorPin,@IsSupervisor,@IsActive, @Note, @CreatedByUserId", conn);
        //        cmd.Parameters.AddWithValue("@EnumeratorCode", data.EnumeratorCode);
        //        cmd.Parameters.AddWithValue("@FullName", data.FullName);
        //        cmd.Parameters.AddWithValue("@EnumeratorType", data.EnumeratorType);
        //        cmd.Parameters.AddWithValue("@EnumeratorPin", data.EnumeratorPin);

        //        cmd.Parameters.AddWithValue("@IsActive", data.IsActive);
        //        cmd.Parameters.AddWithValue("@IsSupervisor", data.IsSupervisor);

        //        cmd.Parameters.AddWithValue("@Note", data.Note);
        //        cmd.Parameters.AddWithValue("@CreatedByUserId", UserId);
        //        await cmd.ExecuteNonQueryAsync();
        //    }
        //    catch (Exception ex)
        //    {

        //    }

        //}

    }
}
