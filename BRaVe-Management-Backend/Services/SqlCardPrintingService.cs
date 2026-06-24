using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.DTOs.claim_session;
using BRaVe_Management_Backend.DTOs.PrintService;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Cryptography;

namespace BRaVe_Management_Backend.Services
{
    public class SqlPrintService : IPrintService
    {
        private readonly string _connectionString;
        private readonly JwsSignerService _signer;
        private readonly AppEnrollmentOptions _opts;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly ILogger<SqlPrintService> _logger;

        public SqlPrintService(
            ISecretProvider secretProvider,
            JwsSignerService signer,
            AppEnrollmentOptions opts,
            IRefreshTokenService refreshTokenService,
            ILogger<SqlPrintService> logger)
        {
            _connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection).Result;

            _signer = signer;
            _opts = opts;
            _refreshTokenService = refreshTokenService;
            _logger = logger;
        }

        public async Task<PrintDeviceClaimResult> ClaimPrintAsync(string sessionCode, string deviceId)
        {
            var codeHash = Crypto.Sha256Bytes(sessionCode);

            using var cn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_PrintDevice_Claim", cn)
            {
                CommandType = CommandType.StoredProcedure
            };

            // INPUTS
            cmd.Parameters.Add("@SessionCodeHash", SqlDbType.VarBinary, 32).Value = codeHash;
            cmd.Parameters.Add("@DeviceId", SqlDbType.VarChar, 50).Value = deviceId;

            // OUTPUTS
            var tenantParam = cmd.Parameters.Add("@TenantId", SqlDbType.Int);
            tenantParam.Direction = ParameterDirection.Output;

            var sessionParam = cmd.Parameters.Add("@SessionId", SqlDbType.BigInt);
            sessionParam.Direction = ParameterDirection.Output;

            await cn.OpenAsync();

            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException ex) when (ex.Number == 50001)
            {
                _logger.LogWarning("Invalid session for code {Code}", sessionCode);
                throw new Exception("Invalid or inactive session code");
            }
            catch (SqlException ex) when (ex.Number == 50002)
            {
                _logger.LogWarning("Device limit reached for code {Code}", sessionCode);
                throw new Exception("Device limit reached");
            }
            catch (SqlException ex) when (ex.Number == 50003)
            {
                _logger.LogWarning("Duplicate device {DeviceId}", deviceId);
                throw new Exception("Device already claimed");
            }

            int tenantId = (int)tenantParam.Value;
            long sessionId = (long)sessionParam.Value;

            //--------------------------------------------------
            // TOKEN GENERATION
            //--------------------------------------------------

            var rawRefreshToken = _refreshTokenService.GenerateToken();

            var refreshTokenHash = SHA256.HashData(
                Convert.FromBase64String(
                    rawRefreshToken.Replace('-', '+')
                                   .Replace('_', '/')
                                   .PadRight((rawRefreshToken.Length + 3) / 4 * 4, '=')
                )
            );

            string accessToken = _signer.CreateEnrollmentJws(
                tenantId: tenantId,
                policyVersion: 1,
                enrollId: $"print:{deviceId}",
                apiBaseUrl: new Uri(
                    Environment.GetEnvironmentVariable(KeyVaultSecretNames.Jwt.API_BASE_URL)
                    ),
                //apiBaseUrl: new Uri(_opts.ApiBaseUrl),
                lifetime: TimeSpan.FromMinutes(43200), //Valid for 30 days
                jti: Guid.NewGuid().ToString(),
                deviceId: deviceId //
            );

            //--------------------------------------------------
            // SAVE REFRESH TOKEN
            //--------------------------------------------------

            using var saveCmd = new SqlCommand(@"
                INSERT INTO tbl_PrintRefreshTokens
                (TenantId, DeviceId, TokenHash, IssuedAtUtc, ExpiresAtUtc)
                VALUES
                (@TenantId, @DeviceId, @Hash, SYSUTCDATETIME(), DATEADD(day, 60, SYSUTCDATETIME()));",
                cn);

            saveCmd.Parameters.AddWithValue("@TenantId", tenantId);
            saveCmd.Parameters.AddWithValue("@DeviceId", deviceId);
            saveCmd.Parameters.AddWithValue("@Hash", refreshTokenHash);

            await saveCmd.ExecuteNonQueryAsync();

            _logger.LogInformation("Print device {DeviceId} claimed session {SessionId}", deviceId, sessionId);

            //--------------------------------------------------
            // RETURN
            //--------------------------------------------------

            return new PrintDeviceClaimResult
            {
                SessionId = sessionId,
                TenantId = tenantId,
                AccessToken = accessToken,
                RefreshToken = rawRefreshToken
            };
        }


        public async Task<RefreshResponse> RefreshAsync(string refreshToken)
        {
            var hash = SHA256.HashData(
                Convert.FromBase64String(
                    refreshToken.Replace('-', '+')
                                .Replace('_', '/')
                                .PadRight((refreshToken.Length + 3) / 4 * 4, '=')
                )
            );

            using var cn = new SqlConnection(_connectionString);
            await cn.OpenAsync();

            var cmd = new SqlCommand(@"
        SELECT TOP 1 TenantId, DeviceId
        FROM tbl_PrintRefreshTokens
        WHERE TokenHash = @Hash
          AND ExpiresAtUtc > SYSUTCDATETIME()", cn);

            cmd.Parameters.AddWithValue("@Hash", hash);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!reader.Read())
                throw new Exception("invalid_or_expired_refresh_token");

            int tenantId = reader.GetInt32(0);
            string deviceId = reader.GetString(1);

            await reader.CloseAsync();

            // ==========================================
            // EXTEND REFRESH TOKEN EXPIRY (SLIDING)
            // ==========================================
            var updateCmd = new SqlCommand(@"
        UPDATE tbl_PrintRefreshTokens
        SET ExpiresAtUtc = DATEADD(day, 60, SYSUTCDATETIME())
        WHERE TokenHash = @Hash", cn);

            updateCmd.Parameters.AddWithValue("@Hash", hash);

            await updateCmd.ExecuteNonQueryAsync();

            // ==========================================
            // GENERATE ACCESS TOKEN
            // ==========================================
            string accessToken = _signer.CreateEnrollmentJws(
                tenantId: tenantId,
                policyVersion: 1,
                enrollId: $"print:refresh:{deviceId}",
               // apiBaseUrl: new Uri(_opts.ApiBaseUrl),
                apiBaseUrl: new Uri(
                     Environment.GetEnvironmentVariable(KeyVaultSecretNames.Jwt.API_BASE_URL)
                    ),
                lifetime: TimeSpan.FromDays(30),
                jti: Guid.NewGuid().ToString()
            );

            return new RefreshResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }


        //LOAD RECORDS FOR PRINTING

        public async Task<List<CardPrintQueueItem>> LoadPrintQueueAsync(int tenantId,List<string>? householdIds,string? activityCode,string deviceId,string user)
        {
            using var cn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_CardPrintQueue_PopulateAndLock", cn)
            {
                CommandType = CommandType.StoredProcedure
            };

            //--------------------------------------------------
            // PARAMETERS
            //--------------------------------------------------

            cmd.Parameters.AddWithValue("@TenantId", tenantId);

            // TVP for HouseholdIds
            var tvp = new DataTable();
            tvp.Columns.Add("HouseholdId", typeof(string));

            if (householdIds != null)
            {
                foreach (var id in householdIds)
                    tvp.Rows.Add(id);
            }

            var tvpParam = cmd.Parameters.AddWithValue("@HouseholdIds", tvp);
            tvpParam.SqlDbType = SqlDbType.Structured;
            tvpParam.TypeName = "dbo.HouseholdIdList";

            cmd.Parameters.AddWithValue("@ActivityCode",
                string.IsNullOrEmpty(activityCode) ? DBNull.Value : activityCode);

            cmd.Parameters.AddWithValue("@DeviceId", deviceId);
            cmd.Parameters.AddWithValue("@User", user);

            //--------------------------------------------------
            // EXECUTE
            //--------------------------------------------------

            await cn.OpenAsync();

            var results = new List<CardPrintQueueItem>();

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                results.Add(new CardPrintQueueItem
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    FullName = reader["FullName"]?.ToString(),
                    FamilySize = Convert.ToInt32(reader["FamilySize"]),
                    HouseholdId = reader["HouseholdId"]?.ToString(),
                    Mission = reader["Mission"]?.ToString(),
                    Program = reader["Program"]?.ToString(),
                    LocationInformation = reader["LocationInformation"]?.ToString(),
                    AdditionalInformation = reader["AdditionalInformation"]?.ToString(),
                    Activity = reader["Activity"]?.ToString(),
                    RegDate = Convert.ToDateTime(reader["RegDate"]),
                    barcodeId = reader["barcodeId"]?.ToString(),

                    CardPrinted = Convert.ToBoolean(reader["CardPrinted"]),
                    PrintedOn = reader["PrintedOn"] as DateTime?,
                    PrintedBy = reader["PrintedBy"]?.ToString(),

                    IsLocked = Convert.ToBoolean(reader["IsLocked"]),
                    LockedBy = reader["LockedBy"]?.ToString(),
                    LockedOn = reader["LockedOn"] as DateTime?,
                    MachineId = reader["MachineId"]?.ToString()
                });
            }

            _logger.LogInformation("Loaded {Count} print queue records for device {DeviceId}", results.Count, deviceId);

            return results;
        }



        // UPDATE PRINTED RECORDS

        public async Task SyncPrintedRecordsAsync(int tenantId,List<string> householdIds,string printedBy)
        {
            using var cn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_CardPrint_SyncPrintedRecords", cn)
            {
                CommandType = CommandType.StoredProcedure
            };

            //--------------------------------------------------
            // PARAMETERS
            //--------------------------------------------------

            cmd.Parameters.AddWithValue("@PrintedBy", printedBy);

            // TVP for HouseholdIds
            var tvp = new DataTable();
            tvp.Columns.Add("HouseholdId", typeof(string));

            foreach (var id in householdIds)
            {
                if (!string.IsNullOrWhiteSpace(id))
                    tvp.Rows.Add(id);
            }

            var tvpParam = cmd.Parameters.AddWithValue("@HouseholdIds", tvp);
            tvpParam.SqlDbType = SqlDbType.Structured;
            tvpParam.TypeName = "dbo.HouseholdIdList";

            //--------------------------------------------------
            // EXECUTE
            //--------------------------------------------------

            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            _logger.LogInformation(
                "Synced {Count} printed records for user {User}",
                householdIds.Count,
                printedBy
            );
        }



        //GET PRINTED RECORDS FOR PORTAL

        public async Task<List<PrintedCardSummaryDto>> GetPrintedRecordsAsync(int tenantId,DateTime? startDate,DateTime? endDate)
        {
            using var cn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_CardPrint_GetPrintedRecords", cn)
            {
                CommandType = CommandType.StoredProcedure
            };

            //--------------------------------------------------
            // PARAMETERS
            //--------------------------------------------------

            cmd.Parameters.AddWithValue("@TenantId", tenantId);
            cmd.Parameters.AddWithValue("@StartDate", (object?)startDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@EndDate", (object?)endDate ?? DBNull.Value);

            //--------------------------------------------------
            // EXECUTE
            //--------------------------------------------------

            await cn.OpenAsync();

            var results = new List<PrintedCardSummaryDto>();

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                results.Add(new PrintedCardSummaryDto
                {
                    CardId = reader.GetInt32(reader.GetOrdinal("CardId")),
                    HouseholdId = reader["HouseholdId"]?.ToString(),
                    FullName = reader["FullName"]?.ToString(),
                    PrintedBy = reader["printedBy"]?.ToString(),
                    PrintedOn = reader["PrintedOn"] as DateTime?,
                    NoOfReprints = Convert.ToInt32(reader["NoOfReprints"])
                });
            }

            return results;
        }




        //UNLOCK LOCKED CARDS

        public async Task UnlockPrintQueueRecordsAsync(List<string> householdIds,string currentUser)
        {
            using var cn = new SqlConnection(_connectionString);

            using var cmd = new SqlCommand(@"
UPDATE tbl_CardPrintQueue
SET
    IsLocked = 0,
    LockedBy = NULL,
    LockedOn = NULL
WHERE HouseholdId IN (
    SELECT HouseholdId
    FROM @HouseholdIds
)
AND LockedBy = @CurrentUser;", cn);

            //--------------------------------------------------
            // TVP
            //--------------------------------------------------

            var tvp = new DataTable();
            tvp.Columns.Add("HouseholdId", typeof(string));

            foreach (var id in householdIds)
            {
                if (!string.IsNullOrWhiteSpace(id))
                    tvp.Rows.Add(id);
            }

            var tvpParam = cmd.Parameters.AddWithValue(
                "@HouseholdIds",
                tvp);

            tvpParam.SqlDbType = SqlDbType.Structured;
            tvpParam.TypeName = "dbo.HouseholdIdList";

            cmd.Parameters.AddWithValue("@CurrentUser",currentUser);

            //--------------------------------------------------
            // EXECUTE
            //--------------------------------------------------


            await cn.OpenAsync();

            await cmd.ExecuteNonQueryAsync();

            _logger.LogInformation(
                "Unlocked {Count} print queue records",
                householdIds.Count);
        }



        //AUTHORIZE CARD REPRINT
        public async Task AuthorizeReprintAsync(int cardId,string authorizedBy,string reason)
        {
            using var cn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_CardPrint_AuthorizeReprint", cn)
            {
                CommandType = CommandType.StoredProcedure
            };

            //--------------------------------------------------
            // PARAMETERS
            //--------------------------------------------------

            cmd.Parameters.AddWithValue("@CardId", cardId);
            cmd.Parameters.AddWithValue("@AuthorizedBy", authorizedBy);
            cmd.Parameters.AddWithValue("@Reason", reason);

            //--------------------------------------------------
            // EXECUTE
            //--------------------------------------------------

            await cn.OpenAsync();

            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException ex) when (ex.Number == 50002)
            {
                throw new Exception("Cannot reprint a card that has not been printed.");
            }
            catch (SqlException ex) when (ex.Number == 50003)
            {
                throw new Exception("An active reprint authorization already exists.");
            }
        }


    }
}