using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.Data.SqlClient;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BRaVe_Management_Backend.Services
{
    public class SqlClaimSessionService : IClaimSessionService
    {
        private string connectionString { get; set; }
        public SqlClaimSessionService(ISecretProvider secretProvider) {

            connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection).Result;
                
                //config.GetConnectionString("DefaultConnection");
        }
 
        public async Task CreateClaimSession(ClaimSessionDto data, string UserId)
        {

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();
              
                var cmd = new SqlCommand(@"sp_CreateEnumerator @EnumeratorCode,@FullName,@EnumeratorPin,@IsSupervisor,@IsActive, @Note, @CreatedByUserId", conn);
                cmd.Parameters.AddWithValue("@Label", data.Label);
                cmd.Parameters.AddWithValue("@SessionCodeHash", data.SessionCodeHash);
                //cmd.Parameters.AddWithValue("@EnumeratorType", data.EnumeratorType);
                cmd.Parameters.AddWithValue("@MaxClaims", data.MaxClaims);

                cmd.Parameters.AddWithValue("@ExpiresAtUtc", data.ExpiresAtUtc);
              
                cmd.Parameters.AddWithValue("@CreatedByUserId", UserId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                string error=ex.Message;
 
            }

        }


        public async Task UpdateClaimSession(ClaimSession data, string UserId)
        {

            try
            {


                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var cmd = new SqlCommand(@"UPDATE tbl_ClaimSessions SET  SessionCodeHash=@SessionCodeHash,Label=@Label,MaxClaims=@MaxClaims, ClaimsIssued=ClaimsIssued,ExpiresAtUtc=@ExpiresAtUtc, Status=@Status,UpdatedByUserId=@UpdatedByUserId, UpdatedOn=GETUTCDATE() WHERE SessionId=@SessionId", conn);
                cmd.Parameters.AddWithValue("@SessionCodeHash", data.SessionCodeHash);
                cmd.Parameters.AddWithValue("@Label", data.Label);
                cmd.Parameters.AddWithValue("@MaxClaims", data.MaxClaims);
                cmd.Parameters.AddWithValue("@ClaimsIssued", data.ClaimsIssued);
                cmd.Parameters.AddWithValue("@ExpiresAtUtc", data.ExpiresAtUtc);
                cmd.Parameters.AddWithValue("@Statuse", data.Status);
                cmd.Parameters.AddWithValue("@SessionId", data.SessionId);
                cmd.Parameters.AddWithValue("@UpdatedByUserId", UserId);
                await cmd.ExecuteNonQueryAsync();
            }

            catch (Exception ex)
            {
              var  Error1 = ex;
            }
        }
        public async Task<IEnumerable<ClaimSession>> GetAllClaimSessions(int tenantId)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = "SELECT SessionId,TenantId,Label,SessionCodeHash,PolicyVersion,MaxClaims,ClaimsIssued,ExpiresAtUtc,Status,CreatedOnUtc,UpdatedOnUtc FROM  tbl_ClaimSessions where TenantId=@TenantId";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@TenantId", tenantId);

                List<ClaimSession> ClaimSessions = new List<ClaimSession>();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var claimSession = new ClaimSession()
                        {

                            SessionId = reader.GetInt64(reader.GetOrdinal("SessionId")),
                            Label = reader.GetString(reader.GetOrdinal("Label")),
                            PolicyVersion = reader.GetInt32(reader.GetOrdinal("PolicyVersion")),

                            MaxClaims = reader.GetInt32(reader.GetOrdinal("MaxClaims")),
                            ClaimsIssued = reader.GetInt32(reader.GetOrdinal("ClaimsIssued")),
                            Status = reader.GetInt32(reader.GetOrdinal("Status")),
                            CreatedOnUtc = reader.GetDateTime(reader.GetOrdinal("CreatedOnUtc")),
                            ExpiresAtUtc = reader.GetDateTime(reader.GetOrdinal("ExpiresAtUtc")),


                             };
                        ClaimSessions.Add(claimSession);

                    }
                }

                return ClaimSessions;
            }
            catch (Exception e)
            {
                return null;
            }
        }
      
 


        //Task<IEnumerable<LookupItemDto>> IEnumeratorService.GetAvailableEnumeratorCodes()
        //{
        //    throw new NotImplementedException();
        //}

        Task<Enumerator?> IClaimSessionService.GetEnumeratorById(int id)
        {
            throw new NotImplementedException();
        }

        Task IClaimSessionService.CreateEnumeratorCodeBatch(EnumeratorCodeBatchDto data, string UserId)
        {
            throw new NotImplementedException();
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

        public async Task RevokeClaimSession(string UserId, int SessionId)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                string query = "UPDATE tbl_ClaimSessions SET ExpiresAtUtc=GETUTCDATE()  WHERE SessionId = @SessionId";

                using var cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@SessionId", SessionId);
                cmd.Parameters.AddWithValue("@UpdatedByUserId", UserId);
 

                await cmd.ExecuteNonQueryAsync();

            }
            catch (Exception e)
            {
                throw e;
            }

        }

        public Task<ClaimSession?> GetClaimSessionById(int id)
        {
            throw new NotImplementedException();
        }
    }
}
