using BRaVe_Mobile_Backend.DTOs;
using BRaVe_Mobile_Backend.Helpers;
using BRaVe_Mobile_Backend.Interfaces;
using System.Data;
using Microsoft.Data.SqlClient;
using static BRaVe_Mobile_Backend.Interfaces.IClaimService;
using BRaVe_Mobile_Backend.Exceptions;
using System.Security.Cryptography;

namespace BRaVe_Mobile_Backend.Services
{
    public class SqlClaimService : IClaimService
    {
        private readonly string connectionString;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly JwsSignerService _signer;
        private readonly ILogger<SqlClaimService> _logger;

        public SqlClaimService(
            ISecretProvider secretProvider,
            IRefreshTokenService refreshTokenService,
            JwsSignerService signer,
            ILogger<SqlClaimService> logger) 
        {
            connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection);
            _refreshTokenService = refreshTokenService;
            _signer = signer;
            _logger = logger;
        }


        public async Task<SessionRecord?> GetActiveSessionAsync(string deviceId, byte[] sessionCodeHash)
        {
            /*const string sql = @"
                SELECT TOP(1) SessionId, TenantId, PolicyVersion, MaxClaims, ClaimsIssued, ExpiresAtUtc
                FROM tbl_ClaimSessions WITH (READCOMMITTED)
                WHERE SessionCodeHash = @H
                  AND Status = 1
                  AND ExpiresAtUtc > SYSUTCDATETIME();";*/

            using var cn = new SqlConnection(connectionString);
            //using var cmd = new SqlCommand(sql, cn);
            using var cmd = new SqlCommand("sp_GetActiveSession", cn)
            {
                CommandType = CommandType.StoredProcedure,
            };

            cmd.Parameters.Add("@DeviceId", SqlDbType.VarChar, 50).Value = deviceId;
            cmd.Parameters.Add("@H", SqlDbType.VarBinary, 32).Value = sessionCodeHash;
            await cn.OpenAsync();
            using var rd = await cmd.ExecuteReaderAsync();
            if (!await rd.ReadAsync())
            {
                _logger.LogInformation("No active session found for hash {Hash}", Convert.ToBase64String(sessionCodeHash));
                return null;
            }
            var session = new SessionRecord(
                rd.GetInt64(0), rd.GetInt32(1), rd.GetInt32(2), rd.GetInt32(3), rd.GetInt32(4), rd.GetDateTime(5));
            _logger.LogInformation("Loaded active session {SessionId} for Tenant {TenantId}", session.SessionId, session.TenantId);
            return session;
        }

        public async Task<ClaimResult> ClaimAsync(ClaimRequest req, string? ip)
        {
            var codeHash = Crypto.Sha256Bytes(req.SessionCode);


            var thumb = new byte[0];

            try
            {
                thumb = Thumbprint.PemToDer(req.DeviceKeyThumbprint);
            }
            catch (Exception e){
                _logger.LogWarning("Device thumbprint parsing failed for session {SessionCode}", req.SessionCode);
                throw new ClaimErrors.NotFound();
            }

            SessionRecord? session;

            try 
            {
                session = await GetActiveSessionAsync(req.DeviceId, codeHash);
            }
            catch (SqlException ex) when (ex.Number == 50003) 
            { 
                throw new ClaimErrors.TenantMismatch(); 
            }

            if (session is null)
            {
                _logger.LogWarning("No active session for hash {Hash}", Convert.ToBase64String(codeHash));
                throw new ClaimErrors.NotFound();
            }

            if (session.ClaimsIssued >= session.MaxClaims)
            {
                _logger.LogWarning("Capacity exceeded for session {SessionId}", session.SessionId);
                throw new ClaimErrors.CapacityExceeded();
            }

            Guid claimRowId;
            string hohprefix;
            int initialId;

            var raw = _refreshTokenService.generateToken();

            var hash = SHA256.HashData(Convert.FromBase64String(
                raw.Replace('-', '+').Replace('_', '/').PadRight((raw.Length + 3) / 4 * 4, '=')));

            try
            {
                (claimRowId,hohprefix,initialId)  = await InsertClaimAndConsumeAsync(session.SessionId, session.TenantId, thumb, Guid.NewGuid().ToString(), DateTime.UtcNow.AddMinutes(15), ip, hash, req.DeviceId);
                _logger.LogInformation("Claim inserted for session {SessionId}, ClaimId {ClaimId}", session.SessionId, claimRowId);
            }
            catch (SqlUniqueConstraintViolationException)
            {
                _logger.LogWarning("Duplicate device claim attempted for session {SessionId}", session.SessionId);
                throw new ClaimErrors.DuplicateDevice();
            }

            string jws = _signer.CreateEnrollmentJws(
                tenantId: session.TenantId,
                deviceId: req.DeviceId,
                enrollId: $"claim:{claimRowId}",
                policyVersion: session.PolicyVersion,
                lifetime: TimeSpan.FromMinutes(15));

            _logger.LogInformation("Enrollment JWS created for session {SessionId}", session.SessionId);
            return new ClaimResult(hohprefix, initialId,jws,raw);
        }

        public async Task<(Guid, string, int)> InsertClaimAndConsumeAsync(long sessionId, int tenantId, byte[] thumb, string jti, DateTime jwsExpUtc, string? ip, byte[] refreshToken, string deviceId)
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("sp_DeviceClaim_InsertAndConsume", cn)
            { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.Add("@SessionId", SqlDbType.BigInt).Value = sessionId;
            cmd.Parameters.Add("@TenantId", SqlDbType.Int).Value = tenantId;
            cmd.Parameters.Add("@DeviceKeyThumbprint", SqlDbType.VarBinary, 2048).Value = thumb;
            cmd.Parameters.Add("@EnrollmentJwsJti", SqlDbType.Char, 36).Value = jti;
            cmd.Parameters.Add("@EnrollmentJwsExpUtc", SqlDbType.DateTime2).Value = jwsExpUtc;
            cmd.Parameters.Add("@RequestIp", SqlDbType.NVarChar, 45).Value = (object?)ip ?? DBNull.Value;
            cmd.Parameters.Add("@RefreshTokenHash", SqlDbType.VarBinary, 32).Value = refreshToken;
            cmd.Parameters.Add("@DeviceId", SqlDbType.VarChar).Value = deviceId;

            var outId = cmd.Parameters.Add("@NewClaimId", SqlDbType.UniqueIdentifier);
            outId.Direction = ParameterDirection.Output;

            var householdPrefix = cmd.Parameters.Add("@SelectedHOHPrefix", SqlDbType.VarChar, 10);
            householdPrefix.Direction = ParameterDirection.Output;

            var initialId = cmd.Parameters.Add("@InitialId", SqlDbType.Int);
            initialId.Direction = ParameterDirection.Output;

            await cn.OpenAsync();
            try
            {
                await cmd.ExecuteNonQueryAsync();
                _logger.LogInformation("Claim row inserted successfully for session {SessionId}", sessionId);
                return ((Guid)outId.Value, (string)householdPrefix.Value, (int)initialId.Value)!;
            }
            catch (SqlException ex) when (ex.Number == 50001) { throw new ClaimErrors.NotFound(); }
            catch (SqlException ex) when (ex.Number == 50002) { throw new ClaimErrors.CapacityExceeded(); }
            catch (SqlException ex) when (ex.Number == 50003) { throw new ClaimErrors.AttestationFailed(); }
            catch (SqlException ex) when (ex.Number == 50004) { throw new ClaimErrors.DuplicateDevice(); }
            catch (SqlException ex) when (ex.Number == 50005) { throw new ClaimErrors.BindingPoolExhausted(); }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601) { throw new ClaimErrors.DuplicateDevice(); }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while inserting claim for session {SessionId}", sessionId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while inserting claim for session {SessionId}", sessionId);
                throw;
            }
        }


    }
}
