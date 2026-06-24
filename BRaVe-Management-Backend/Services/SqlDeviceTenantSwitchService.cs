using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using DocumentFormat.OpenXml.Bibliography;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace BRaVe_Management_Backend.Services
{
    public class SqlDeviceTenantSwitchService : IDeviceProvisioningMonitoringService
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlDeviceTenantSwitchService> _logger;

        public SqlDeviceTenantSwitchService(
            ISecretProvider secretProvider,
            ILogger<SqlDeviceTenantSwitchService> logger)
        {
            _logger = logger;
            _connectionString = secretProvider
                .GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection)
                .Result;
        }

        public async Task<List<DeviceProvisioningErrorGridRowDto>> GetProvisioningErrorsAsync(string? deviceId = null)
        {
            var result = new List<DeviceProvisioningErrorGridRowDto>();

            try
            {
                await using var connection = new SqlConnection(_connectionString);
                await using var command = new SqlCommand("dbo.sp_GetLatestDeviceTenantSwitch", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                // Pass parameter; DBNull if null
                command.Parameters.Add("@DeviceId", SqlDbType.VarChar, 50)
                       .Value = (object?)deviceId ?? DBNull.Value;

                await connection.OpenAsync();

                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    result.Add(new DeviceProvisioningErrorGridRowDto
                    {
                        DeviceId = reader["DeviceId"]?.ToString() ?? string.Empty,
                        OldTenantName = reader["OldTenantName"]?.ToString() ?? string.Empty,
                        OldTenantId = Convert.ToInt32(reader["OldTenantId"]),
                        CurrentTenantName = reader["NewTenantName"]?.ToString() ?? string.Empty,
                        CurrentTenantId = Convert.ToInt32(reader["NewTenantId"]),
                        FirstAttempt = reader["FirstAttempt"] as DateTime?,
                        LastAttempt = reader["LastAttempt"] as DateTime?,
                        AttemptCount = reader["AttemptCount"] as int?
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving device provisioning errors for DeviceId: {DeviceId}", deviceId);
                throw;
            }

            return result;
        }

        public async Task DetachDeviceAsync(
        string deviceId,
        int currentTenantId,
        int oldTenantId,
        string detachReason,
        string approvedBy)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand("dbo.sp_DetachDeviceFromTenant", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                command.Parameters.Add("@DeviceId", SqlDbType.VarChar, 50)
                    .Value = deviceId;

                command.Parameters.Add("@CurrentTenantId", SqlDbType.Int)
                    .Value = currentTenantId;

                command.Parameters.Add("@OldTenantId", SqlDbType.Int)
                    .Value = oldTenantId;

                command.Parameters.Add("@Reason", SqlDbType.NVarChar, 500)
                    .Value = string.IsNullOrWhiteSpace(detachReason)
                                ? string.Empty
                                : detachReason;

                command.Parameters.Add("@ApprovedBy", SqlDbType.VarChar, 100)
                    .Value = string.IsNullOrWhiteSpace(approvedBy)
                                ? "System"
                                : approvedBy;

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error detaching device {DeviceId} from tenant {OldTenantId}",
                    deviceId,
                    oldTenantId);

                throw;
            }
        }
    }
}