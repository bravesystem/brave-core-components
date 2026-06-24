using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Exceptions;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.DTOs.claim_session;
using BRaVe_Management_Backend.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using static BRaVe_Management_Backend.Interfaces.IClaimService;

namespace BRaVe_Management_Backend.Services
{
    public class SqlClaimService : IClaimService
    {
        private readonly string connectionString;
        private readonly IAttestationVerifier _attest;
        private readonly JwsSignerService _signer;
        private readonly AppEnrollmentOptions _opts;
        private readonly ILogger<SqlClaimService> _logger;

        public SqlClaimService(
            ISecretProvider secretProvider,
            IAttestationVerifier attest,
            JwsSignerService signer,
            AppEnrollmentOptions opts,
            ILogger<SqlClaimService> logger) // <-- Inject ILogger<T>
        {
            connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection).Result;

            _attest = attest;
            _signer = signer;
            _opts = opts;
            _logger = logger;
        }

        public async Task<long> CreateAsync(ClaimSessionRecord csr)
        {
            const string sql = @"
                INSERT INTO tbl_ClaimSessions
                  (TenantId, Label, SessionCodeHash, PolicyVersion, MaxClaims, ClaimsIssued, ExpiresAtUtc, Status, CreatedOnUtc)
                VALUES
                  (@TenantId, @Label, @SessionCodeHash, @PolicyVersion, @MaxClaims, 0, @ExpiresAtUtc, 1, SYSUTCDATETIME());
                SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.Add("@TenantId", SqlDbType.Int).Value = csr.TenantId;
            cmd.Parameters.Add("@Label", SqlDbType.NVarChar, 100).Value = (object?)csr.Label ?? DBNull.Value;
            cmd.Parameters.Add("@SessionCodeHash", SqlDbType.VarBinary, 32).Value = csr.SessionCodeHash;
            cmd.Parameters.Add("@PolicyVersion", SqlDbType.Int).Value = csr.PolicyVersion;
            cmd.Parameters.Add("@MaxClaims", SqlDbType.Int).Value = csr.MaxClaims;
            cmd.Parameters.Add("@ExpiresAtUtc", SqlDbType.DateTime2).Value = csr.ExpiresAtUtc;

            await cn.OpenAsync();

            try
            {
                var idObj = await cmd.ExecuteScalarAsync();
                _logger.LogInformation("Created ClaimSession for Tenant {TenantId} with ID {Id}", csr.TenantId, idObj);
                return (long)idObj!;
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                _logger.LogWarning(ex, "Unique constraint violation when creating ClaimSession for Tenant {TenantId}", csr.TenantId);
                throw new SqlUniqueConstraintViolationException(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ClaimSession for Tenant {TenantId}", csr.TenantId);
                throw;
            }
        }

        public async Task<SessionRecord?> GetActiveSessionAsync(byte[] sessionCodeHash)
        {
            const string sql = @"
                SELECT TOP(1) SessionId, TenantId, PolicyVersion, MaxClaims, ClaimsIssued, ExpiresAtUtc
                FROM tbl_ClaimSessions WITH (READCOMMITTED)
                WHERE SessionCodeHash = @H
                  AND Status = 1
                  AND ExpiresAtUtc > SYSUTCDATETIME();";

            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand(sql, cn);
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
            if (!Thumbprint.TryParse(req.DeviceKeyThumbprint, out var thumb))
            {
                _logger.LogWarning("Device thumbprint parsing failed for session {SessionCode}", req.SessionCode);
                throw new ClaimErrors.NotFound();
            }

            var session = await GetActiveSessionAsync(codeHash);
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

            bool ok = await _attest.VerifyHardwareAsync(req.Attestation, thumb);
            if (!ok)
            {
                _logger.LogWarning("Hardware attestation failed for Device {Thumb}", thumb);
                throw new ClaimErrors.AttestationFailed();
            }

            Guid claimRowId;
            try
            {
                claimRowId = await InsertClaimAndConsumeAsync(session.SessionId, session.TenantId, thumb, Guid.NewGuid().ToString(), DateTime.UtcNow.AddMinutes(15), ip);
                _logger.LogInformation("Claim inserted for session {SessionId}, ClaimId {ClaimId}", session.SessionId, claimRowId);
            }
            catch (SqlUniqueConstraintViolationException)
            {
                _logger.LogWarning("Duplicate device claim attempted for session {SessionId}", session.SessionId);
                throw new ClaimErrors.DuplicateDevice();
            }

            string jws = _signer.CreateEnrollmentJws(
                tenantId: session.TenantId,
                policyVersion: session.PolicyVersion,
                enrollId: $"claim:{claimRowId}",
                apiBaseUrl: new Uri(_opts.ApiBaseUrl),
                lifetime: TimeSpan.FromMinutes(15),
                jti: Guid.NewGuid().ToString());

            _logger.LogInformation("Enrollment JWS created for session {SessionId}", session.SessionId);
            return new ClaimResult(jws);
        }

        public async Task<Guid> InsertClaimAndConsumeAsync(long sessionId, int tenantId, byte[] thumb, string jti, DateTime jwsExpUtc, string? ip)
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("sp_DeviceClaim_InsertAndConsume", cn)
            { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.Add("@SessionId", SqlDbType.BigInt).Value = sessionId;
            cmd.Parameters.Add("@TenantId", SqlDbType.Int).Value = tenantId;
            cmd.Parameters.Add("@DeviceKeyThumbprint", SqlDbType.VarBinary, 32).Value = thumb;
            cmd.Parameters.Add("@EnrollmentJwsJti", SqlDbType.Char, 36).Value = jti;
            cmd.Parameters.Add("@EnrollmentJwsExpUtc", SqlDbType.DateTime2).Value = jwsExpUtc;
            cmd.Parameters.Add("@RequestIp", SqlDbType.NVarChar, 45).Value = (object?)ip ?? DBNull.Value;

            var outId = cmd.Parameters.Add("@NewClaimId", SqlDbType.UniqueIdentifier);
            outId.Direction = ParameterDirection.Output;

            await cn.OpenAsync();
            try
            {
                await cmd.ExecuteNonQueryAsync();
                _logger.LogInformation("Claim row inserted successfully for session {SessionId}", sessionId);
                return (Guid)outId.Value!;
            }
            catch (SqlException ex) when (ex.Number == 50001) { throw new ClaimErrors.NotFound(); }
            catch (SqlException ex) when (ex.Number == 50002) { throw new ClaimErrors.CapacityExceeded(); }
            catch (SqlException ex) when (ex.Number == 50003) { throw new ClaimErrors.AttestationFailed(); }
            catch (SqlException ex) when (ex.Number == 50004) { throw new ClaimErrors.DuplicateDevice(); }
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

//**********************************Print App Claims********************************************************************************
        public async Task<long> CreatePrintSessionAsync(PrintSessionRecordCreate psr)
        {
            const string sql = @"
        INSERT INTO tbl_PrintClaimSessions
        (TenantId, Label, SessionCodeHash, MaxDevices, DevicesConnected, Status, CreatedOnUtc)
        VALUES
        (@TenantId, @Label, @SessionCodeHash, @MaxDevices, 0, 1, SYSUTCDATETIME());

        SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand(sql, cn);

            cmd.Parameters.Add("@TenantId", SqlDbType.Int).Value = psr.TenantId;
            cmd.Parameters.Add("@Label", SqlDbType.NVarChar, 100).Value = (object?)psr.Label ?? DBNull.Value;
            cmd.Parameters.Add("@SessionCodeHash", SqlDbType.VarBinary, 32).Value = psr.SessionCodeHash;
            cmd.Parameters.Add("@MaxDevices", SqlDbType.Int).Value = psr.MaxDevices;

            await cn.OpenAsync();

            try
            {
                var idObj = await cmd.ExecuteScalarAsync();
                _logger.LogInformation("Created PrintSession for Tenant {TenantId} with ID {Id}", psr.TenantId, idObj);
                return (long)idObj!;
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                _logger.LogWarning(ex, "Duplicate SessionCodeHash for Tenant {TenantId}", psr.TenantId);
                throw new SqlUniqueConstraintViolationException(ex);
            }
        }

        public async Task<PrintSessionRecord?> GetActivePrintSessionAsync(byte[] sessionCodeHash)
        {
            const string sql = @"
        SELECT TOP(1) SessionId, TenantId, MaxDevices, DevicesConnected
        FROM tbl_PrintClaimSessions
        WHERE SessionCodeHash = @H
        AND Status = 1;";

            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand(sql, cn);

            cmd.Parameters.Add("@H", SqlDbType.VarBinary, 32).Value = sessionCodeHash;

            await cn.OpenAsync();
            using var rd = await cmd.ExecuteReaderAsync();

            if (!await rd.ReadAsync())
                return null;

            return new PrintSessionRecord(
                rd.GetInt64(0),
                rd.GetInt32(1),
                rd.GetInt32(2),
                rd.GetInt32(3)
            );
        }

        public async Task<PrintClaimResult> ClaimPrintAsync(string sessionCode, string deviceId)
        {
            var codeHash = Crypto.Sha256Bytes(sessionCode);

            var session = await GetActivePrintSessionAsync(codeHash);
            if (session is null)
                throw new Exception("Invalid or inactive session");

            if (session.DevicesConnected >= session.MaxDevices)
                throw new Exception("Device limit reached");

            using var cn = new SqlConnection(connectionString);
            await cn.OpenAsync();

            using var tx = cn.BeginTransaction();

            try
            {
                // Register device
                var insertDevice = new SqlCommand(@"
            INSERT INTO tbl_PrintDevices (SessionId, DeviceId, CreatedOnUtc)
            VALUES (@SessionId, @DeviceId, SYSUTCDATETIME());", cn, tx);

                insertDevice.Parameters.AddWithValue("@SessionId", session.SessionId);
                insertDevice.Parameters.AddWithValue("@DeviceId", deviceId);

                await insertDevice.ExecuteNonQueryAsync();

                // Increment counter
                var updateSession = new SqlCommand(@"
            UPDATE tbl_PrintSessions
            SET DevicesConnected = DevicesConnected + 1
            WHERE SessionId = @SessionId;", cn, tx);

                updateSession.Parameters.AddWithValue("@SessionId", session.SessionId);

                await updateSession.ExecuteNonQueryAsync();

                tx.Commit();

                _logger.LogInformation("Device {DeviceId} claimed print session {SessionId}", deviceId, session.SessionId);

                return new PrintClaimResult
                {
                    SessionId = session.SessionId,
                    TenantId = session.TenantId
                };
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task<List<PrintSessionDto>> GetPrintSessionsAsync(int tenantId)
        {
            const string sql = @"
        SELECT SessionId, Label, MaxDevices, DevicesConnected, Status, CreatedOnUtc
        FROM tbl_PrintClaimSessions
        WHERE TenantId = @TenantId
        ORDER BY CreatedOnUtc DESC;";

            var result = new List<PrintSessionDto>();

            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand(sql, cn);

            cmd.Parameters.AddWithValue("@TenantId", tenantId);

            await cn.OpenAsync();
            using var rd = await cmd.ExecuteReaderAsync();

            while (await rd.ReadAsync())
            {
                result.Add(new PrintSessionDto
                {
                    SessionId = rd.GetInt64(0),
                    Label = rd.IsDBNull(1) ? "" : rd.GetString(1),
                    MaxDevices = rd.GetInt32(2),
                    DevicesConnected = rd.GetInt32(3),
                    Status = rd.GetInt32(4),
                    CreatedOnUtc = rd.GetDateTime(5)
                });
            }

            return result;
        }

        public async Task RevokePrintSessionAsync(long sessionId, int tenantId)
        {
            const string sql = @"
        UPDATE tbl_PrintClaimSessions
        SET Status = 2, UpdatedOnUtc = SYSUTCDATETIME()
        WHERE SessionId = @SessionId
        AND TenantId = @TenantId;";

            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand(sql, cn);

            cmd.Parameters.AddWithValue("@SessionId", sessionId);
            cmd.Parameters.AddWithValue("@TenantId", tenantId);

            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
