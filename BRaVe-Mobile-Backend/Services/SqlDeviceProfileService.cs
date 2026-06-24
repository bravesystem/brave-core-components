using BRaVe_Mobile_Backend.Helpers;
using BRaVe_Mobile_Backend.Interfaces;
using BRaVe_Mobile_Backend.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using static BRaVe_Mobile_Backend.Interfaces.IClaimService;

namespace BRaVe_Mobile_Backend.Services
{
    public class SqlDeviceProfileService : IDeviceService
    {

        private readonly string connectionString;
        private readonly ILogger<SqlClaimService> _logger;

        public SqlDeviceProfileService(
            ISecretProvider secretProvider,
            ILogger<SqlClaimService> logger)
        {
            connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection);
            _logger = logger;
        }


        public async Task<DeviceProfile> GetByIdAsync(string deviceId)
        {
            const string sql = @"
                SELECT TOP(1) HOHPrefix, TenantId
                FROM tbl_DeviceHouseholdBindings WITH (READCOMMITTED)
                WHERE DeviceId = @Id;";

            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.Add("@Id", SqlDbType.VarChar, 50).Value = deviceId;
            await cn.OpenAsync();
            using var rd = await cmd.ExecuteReaderAsync();
            if (!await rd.ReadAsync())
            {
                _logger.LogInformation($"No active device profile found with Id {deviceId}");
                return null;
            }

            return new DeviceProfile { DeviceId = deviceId , HouseholdPrefix = rd.GetString(rd.GetOrdinal("HOHPrefix")), TenantId = rd.GetInt32(rd.GetOrdinal("TenantId")) }; 

        }
    }
}
