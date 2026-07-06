using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace BRaVe_Management_Backend.Services
{
    public class SqlFileBasedDeduplicationService : IFileBasedDeduplicationService
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlFileBasedDeduplicationService> _logger;
        //private readonly IBackgroundTaskQueue _taskQueue;
        //private readonly IServiceScopeFactory _scopeFactory;
        private readonly IEncryptionService _encryptionService;

        //SqlFailedBatchService

        public SqlFileBasedDeduplicationService(IEncryptionService encryptionService,ISecretProvider secretProvider,  ILogger<SqlFileBasedDeduplicationService> logger)
        {
            _encryptionService = encryptionService;
            _logger = logger;
            _connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection).Result;
        }

        public async Task<IReadOnlyList<UploadedFile>> GetUploadJobsAsync(
        int tenantId,
        int maxCount = 10,
        CancellationToken cancellationToken = default)
        {
            if (maxCount < 1)
            {
                maxCount = 10;
            }

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand("sp_FileBasedDeduplication_GetUploadJobs", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@TenantId", SqlDbType.Int) { Value = tenantId });
            command.Parameters.Add(new SqlParameter("@MaxCount", SqlDbType.Int) { Value = maxCount });

            var uploads = new List<UploadedFile>();

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                uploads.Add(new UploadedFile
                {
                    JobId = reader.GetGuid(reader.GetOrdinal("JobId")),
                    Filename = reader.GetString(reader.GetOrdinal("Filename")),
                    UploadedBy = reader.GetString(reader.GetOrdinal("UploadedBy")),
                    UploadedOn = reader.GetDateTime(reader.GetOrdinal("UploadedOn")),
                    IsProcessed = reader.GetBoolean(reader.GetOrdinal("IsProcessed")),
                    ProcessedOn = reader.IsDBNull(reader.GetOrdinal("ProcessedOn"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("ProcessedOn"))
                });
            }

            return uploads;
        }

        public async Task<FileBasedDeduplicationProcessResult> ProcessUploadAsync(int tenantId, string json, string filename, string uploadedBy, CancellationToken cancellationToken = default)
        {

            ArgumentException.ThrowIfNullOrWhiteSpace(json);
            ArgumentException.ThrowIfNullOrWhiteSpace(filename);
            ArgumentException.ThrowIfNullOrWhiteSpace(uploadedBy);

            var biometricTemplates = BuildBiometricTemplateTable(json);


            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand("sp_FileBasedDeduplication_ProcessUpload", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@TenantId", SqlDbType.Int) { Value = tenantId });
            command.Parameters.Add(new SqlParameter("@Json", SqlDbType.NVarChar, -1) { Value = json });
            command.Parameters.Add(new SqlParameter("@Filename", SqlDbType.NVarChar, 260) { Value = filename });
            command.Parameters.Add(new SqlParameter("@UploadedBy", SqlDbType.NVarChar, 256) { Value = uploadedBy });

            var biometricsParameter = new SqlParameter("@Biometrics", SqlDbType.Structured)
            {
                TypeName = "dbo.BiometricTvp",
                Value = biometricTemplates
            };
            command.Parameters.Add(biometricsParameter);

            var jobIdParameter = new SqlParameter("@JobId", SqlDbType.UniqueIdentifier)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(jobIdParameter);

            await command.ExecuteNonQueryAsync(cancellationToken);

            var jobId = jobIdParameter.Value is Guid guid
                ? guid
                : throw new InvalidOperationException("Upload procedure did not return a job identifier.");

            return new FileBasedDeduplicationProcessResult
            {
                JobId = jobId,
                Message = $"\"{filename}\" was uploaded successfully."
            };
        }


        private DataTable BuildBiometricTemplateTable(string json)
        {
            var table = new DataTable();
            table.Columns.Add("BiometricId", typeof(Guid));
            table.Columns.Add("BiometricData", typeof(byte[]));

            using var document = JsonDocument.Parse(json);

            foreach (var household in document.RootElement.EnumerateArray())
            {
                if (!household.TryGetProperty("householdMembers", out var members) ||
                    members.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var member in members.EnumerateArray())
                {
                    if (!member.TryGetProperty("biometricType", out var biometricTypeElement))
                    {
                        continue;
                    }

                    var biometricType = biometricTypeElement.GetString();
                    if (string.IsNullOrEmpty(biometricType) ||
                        string.Equals(biometricType, "none", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!member.TryGetProperty("uuid", out var uuidElement) ||
                        !Guid.TryParse(uuidElement.GetString(), out var memberUuid))
                    {
                        continue;
                    }

                    var biometricRawTemplateBase64 = member.TryGetProperty("biometricRawTemplateBase64", out var base64Element)
                        ? base64Element.GetString() ?? string.Empty
                        : string.Empty;

                    byte[] unencryptedbytes = GetBytes(biometricRawTemplateBase64);

                    /*byte[] encryptedbytes = _encryptionService.Encrypt(unencryptedbytes);

                    byte[] test = _encryptionService.Decrypt(encryptedbytes);

                    bool areNotEqual = ByteArraysEqual(encryptedbytes, unencryptedbytes);
                    bool areEqual = ByteArraysEqual(test, unencryptedbytes);*/

                    table.Rows.Add(memberUuid, _encryptionService.Encrypt(unencryptedbytes));
                }
            }

            return table;
        }

        public bool ByteArraysEqual(byte[]? a, byte[]? b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (a == null || b == null)
                return false;

            if (a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                    return false;
            }

            return true;
        }

        private byte[] GetBytes(string biometricRawTemplateBase64)
        {
            // TODO: decode biometricRawTemplateBase64 into template bytes.
            if (!string.IsNullOrEmpty(biometricRawTemplateBase64))
            {
                return Convert.FromBase64String(biometricRawTemplateBase64);
            }

            return Array.Empty<byte>();
        }

    }
}
