using Azure.Core;
using BRaVe_Mobile_Backend.Helpers;
using BRaVe_Mobile_Backend.Interfaces;
using BRaVe_Mobile_Backend.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BRaVe_Mobile_Backend.Services
{
    public class SqlEnumeratorService : IEnumeratorService
    {
        private readonly string connectionString;
        private readonly ILogger<SqlPubService> _logger;

        public SqlEnumeratorService(
            ISecretProvider secretProvider,
            ILogger<SqlPubService> logger)
        {
            connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection);

            _logger = logger;
        }

        public async Task<List<Enumerator>> GetAll(string DeviceId, int TenantId, string? RequestIp, int Flag)
        {
            var enumerators = new List<Enumerator>();

            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand("sp_RefreshEnumeratorList", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add(new SqlParameter("@DeviceId", SqlDbType.VarChar, 50) { Value = DeviceId });
                cmd.Parameters.Add(new SqlParameter("@TenantId", SqlDbType.Int) { Value = TenantId });
                cmd.Parameters.Add(new SqlParameter("@RequestIp", SqlDbType.NVarChar, 45) { Value = (object?)RequestIp ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@Flag", SqlDbType.Int) { Value = Flag });

                await conn.OpenAsync();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        string type = reader["EnumeratorType"].ToString();

                        var enumerator = new Enumerator
                        {
                            EnumeratorCode = reader["EnumeratorCode"].ToString(),
                            FullName = reader["FullName"].ToString(),
                            PhotoBase64 = reader["PhotoBase64"] as string,
                            Note = reader["Note"] as string,
                            IsSupervisor = "S".Equals(reader["EnumeratorType"].ToString(), StringComparison.OrdinalIgnoreCase),
                            IsActive = reader["IsActive"] != DBNull.Value && (bool)reader["IsActive"],
                            IsPinUpdated = reader["IsPinUpdated"] != DBNull.Value && (bool)reader["IsPinUpdated"],
                            EnumeratorPin = reader["EnumeratorPin"] as byte[], // VARBINARY
                            UpdatedOn = reader["UpdatedOn"] != DBNull.Value ? (DateTime)reader["UpdatedOn"] : DateTime.MinValue
                        };

                        enumerators.Add(enumerator);
                    }
                }
            }

            return enumerators;
        }

        public async Task<List<Enumerator>> SetPin(SetPinRequest data)
        {

            var enumerators = new List<Enumerator>();

            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand("sp_SetEnumeratorPin", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add(new SqlParameter("@DeviceId", SqlDbType.VarChar, 50) { Value = data.DeviceId });
                cmd.Parameters.Add(new SqlParameter("@RequestIp", SqlDbType.NVarChar, 45) { Value = (object?)data.RequestIp ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@TenantId", SqlDbType.Int) { Value = data.TenantId });
                cmd.Parameters.Add(new SqlParameter("@Code", SqlDbType.VarChar, 50) { Value = data.Code });
                cmd.Parameters.Add(new SqlParameter("@OldPin", SqlDbType.VarBinary, 32) { Value = data.OldPin });
                cmd.Parameters.Add(new SqlParameter("@NewPin", SqlDbType.VarBinary, 32) { Value = data.NewPin });

                await conn.OpenAsync();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        string type = reader["EnumeratorType"].ToString();

                        var enumerator = new Enumerator
                        {
                            EnumeratorCode = reader["EnumeratorCode"].ToString(),
                            FullName = reader["FullName"].ToString(),
                            PhotoBase64 = reader["PhotoBase64"] as string,
                            Note = reader["Note"] as string,
                            IsSupervisor = "S".Equals(reader["EnumeratorType"].ToString(), StringComparison.OrdinalIgnoreCase),
                            IsActive = reader["IsActive"] != DBNull.Value && (bool)reader["IsActive"],
                            IsPinUpdated = reader["IsPinUpdated"] != DBNull.Value && (bool)reader["IsPinUpdated"],
                            EnumeratorPin = reader["EnumeratorPin"] as byte[], // VARBINARY
                            UpdatedOn = reader["UpdatedOn"] != DBNull.Value ? (DateTime)reader["UpdatedOn"] : DateTime.MinValue
                        };

                        enumerators.Add(enumerator);
                    }
                }
            }

            return enumerators;
        }
    }
}
