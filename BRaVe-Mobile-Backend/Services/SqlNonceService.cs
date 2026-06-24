using BRaVe_Mobile_Backend.Exceptions;
using BRaVe_Mobile_Backend.Helpers;
using BRaVe_Mobile_Backend.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BRaVe_Mobile_Backend.Services
{
    public class SqlNonceService : INonceService
    {


        private readonly string connectionString;
        private readonly ILogger<SqlPubService> _logger;

        public SqlNonceService(
            ISecretProvider secretProvider,
            ILogger<SqlPubService> logger)
        {
            connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection);

            _logger = logger;
        }



        public async Task saveNonce(Guid nonceId, string deviceId, byte[] hashedNonce, string? purpose, TimeSpan ttl, string? metadata)
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("sp_InsertChallenge", cn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@ChallengeId", SqlDbType.UniqueIdentifier).Value = nonceId;
            cmd.Parameters.Add("@Purpose", SqlDbType.NVarChar, 64).Value = (object?)purpose ?? DBNull.Value;
            cmd.Parameters.Add("@RequesterId", SqlDbType.VarChar, 50).Value = deviceId;
            cmd.Parameters.Add("@RequesterType", SqlDbType.Int).Value = 1; // Assuming fixed type
            cmd.Parameters.Add("@NonceHash", SqlDbType.VarBinary, 32).Value = hashedNonce;
            cmd.Parameters.Add("@TTLMinutes", SqlDbType.Int).Value = (int)ttl.TotalMinutes;
            cmd.Parameters.Add("@Metadata", SqlDbType.NVarChar, 256).Value = (object?)metadata ?? DBNull.Value;


            await cn.OpenAsync();
            try
            {
                await cmd.ExecuteNonQueryAsync();
                _logger.LogInformation($"Nonce added successfully for requester Id: {deviceId}");
                
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, $"SQL error while adding nonce  claim for for requester Id: {deviceId}");
                throw;
            }
        }

        public async Task<bool> validateNonce(Guid nonceId, byte[] hashedNonce, string? UsedByIp)
        {

            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("sp_ValidateNonce", cn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@ChallengeId", SqlDbType.UniqueIdentifier).Value = nonceId;
            cmd.Parameters.Add("@NonceHash", SqlDbType.VarBinary, 32).Value = hashedNonce;
            cmd.Parameters.Add("@UsedByIp", SqlDbType.NVarChar, 45).Value = UsedByIp;

            cn.Open();
            var result = cmd.ExecuteScalar();
            return result != null && Convert.ToInt32(result) == 1;

        }
    }
}
