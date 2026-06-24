using BRaVe_Mobile_Backend.DTOs;
using BRaVe_Mobile_Backend.Exceptions;
using BRaVe_Mobile_Backend.Helpers;
using BRaVe_Mobile_Backend.Interfaces;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Security.Cryptography;

namespace BRaVe_Mobile_Backend.Services
{
    public class SqlRefreshTokenService : IRefreshTokenService
    {
        private readonly string connectionString;
        private readonly ILogger<SqlClaimService> _logger;

        public SqlRefreshTokenService(
            ISecretProvider secretProvider,
            ILogger<SqlClaimService> logger)
        {
            connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection);

            _logger = logger;
        }

        public string generateToken()
        {
            // 32 bytes random, return as base64url; store SHA-256
            Span<byte> rnd = stackalloc byte[32];
            RandomNumberGenerator.Fill(rnd);
            var raw = Convert.ToBase64String(rnd).TrimEnd('=').Replace('+', '-').Replace('/', '_');

            return raw;

        }

        public async Task<RefreshTokenTenantPair> RefreshAsync(TokenRefreshDto dto)
        {

            // 3) Call proc to consume+rotate and fetch user/device

            using var cn = new SqlConnection(connectionString);

            int TenantId = 0;

            using (var cmd = new SqlCommand("dbo.Sp_RefreshToken_ConsumeRotate", cn) { CommandType = CommandType.StoredProcedure })
            {
                cmd.Parameters.Add("@TokenHash", SqlDbType.VarBinary, 32).Value = dto.TokenHash;
                cmd.Parameters.Add("@NewTokenHash", SqlDbType.VarBinary, 32).Value = dto.FreshToken;
                cmd.Parameters.Add("@NowUtc", SqlDbType.DateTime2).Value = DateTime.UtcNow;
                cmd.Parameters.Add("@NewExpiresUtc", SqlDbType.DateTime2).Value = DateTime.UtcNow.AddDays(30);

                var pU = cmd.Parameters.Add("@UserId", SqlDbType.VarChar).Value = dto.Enumerator;
                var pD = cmd.Parameters.Add("@DeviceId", SqlDbType.VarChar).Value = dto.DeviceId;

                var tenantId = cmd.Parameters.Add("@TenantId", SqlDbType.Int);
                tenantId.Direction = ParameterDirection.Output;

                await cn.OpenAsync();
                try
                {

                    await cmd.ExecuteNonQueryAsync();
                    TenantId = (int)tenantId.Value;


                }
                catch (SqlException ex) when (ex.Number == 51001)
                {
                    throw new RefreshErrors.NotFound();
                }
                catch (SqlException ex) when (ex.Number == 51002)
                {
                   throw new RefreshErrors.Invalid();
                }
                catch (SqlException ex)
                {
                    throw new RefreshErrors.Invalid();
                }
                catch (Exception ex)
                {
                    throw ex;
                }

            }

            RefreshTokenTenantPair response = new RefreshTokenTenantPair();
            response.Refresh = dto.FreshRaw;
            response.Tenant = TenantId;

            return response;
        }
    }
}
