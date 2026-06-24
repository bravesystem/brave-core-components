using BRaVe_Mobile_Backend.Helpers;
using BRaVe_Mobile_Backend.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;
using static BRaVe_Mobile_Backend.Interfaces.IClaimService;

namespace BRaVe_Mobile_Backend.Services
{
    public class SqlPubService : IPubService
    {

        private readonly string connectionString;
        private readonly ILogger<SqlPubService> _logger;

        public SqlPubService(
            ISecretProvider secretProvider,
            ILogger<SqlPubService> logger)
        {
            connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection);

            _logger = logger;
        }
        public async Task<byte[]> getClientPublickey(string DeviceId)
        {
            try
            {

                const string sql = @"
                select top(1) c.DeviceKeyThumbprint from tbl_Devices d 
                join tbl_DeviceClaims c on d.ClaimId=c.ClaimId
                where d.DeviceId=@Id";

                using var cn = new SqlConnection(connectionString);
                using var cmd = new SqlCommand(sql, cn);
                cmd.Parameters.Add("@Id", SqlDbType.VarChar, 32).Value = DeviceId;

                await cn.OpenAsync();

                using var rd = await cmd.ExecuteReaderAsync();
                if (!await rd.ReadAsync())
                {
                    _logger.LogInformation($"No thumbprint found for device {DeviceId}");
                    return null;
                }

                byte[] derBytes = (byte[])rd["DeviceKeyThumbprint"];

                return derBytes;

                // Get the length of the data
                //long length = rd.GetBytes(0, 0, null, 0, 0);
                //byte[] pk = new byte[length];

                //// Read the data into the buffer
                //rd.GetBytes(0, 0, pk, 0, (int)length);

                //return pk;

            }
            catch (Exception e)
            {
                throw e;
            }
            
        }
    }
}
