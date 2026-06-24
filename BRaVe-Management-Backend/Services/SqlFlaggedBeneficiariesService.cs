using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BRaVe_Management_Backend.Services
{
    public class SqlFlaggedBeneficiariesService : IFlaggedBeneficiariesService
    {

        private readonly char[] AlphaNumericChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

        private readonly Random Random = new();
        private readonly string _connectionString;

        private readonly ILogger<SqlFlaggedBeneficiariesService> _logger;

        public SqlFlaggedBeneficiariesService(
            ISecretProvider secretProvider,
            ILogger<SqlFlaggedBeneficiariesService> logger)
        {
            _logger = logger;

            _connectionString = secretProvider
                .GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection)
                .Result;
        }


        public async Task InsertFlaggedBeneficiaries(CreateFlaggedBeneficiaryRequest request)
        {
            using var conn = new SqlConnection(_connectionString);

            using var cmd = new SqlCommand(
                "dbo.sp_InsertFlaggedBeneficiaries",
                conn);

            cmd.CommandType = CommandType.StoredProcedure;

            var table = new DataTable();

            table.Columns.Add("BeneficiaryId", typeof(Guid));
            table.Columns.Add("ActivityCode", typeof(string));

            request.Beneficiaries.ForEach(x =>
            {
                table.Rows.Add(
                    x.BeneficiaryId,
                    x.ActivityCode);
            });

            var tvpParam = cmd.Parameters.AddWithValue(
                "@Beneficiaries",
                table);

            tvpParam.SqlDbType = SqlDbType.Structured;
            tvpParam.TypeName = "dbo.FlaggedBeneficiaryType";

            cmd.Parameters.AddWithValue(
                "@Reason",
                (object?)request.Reason ?? DBNull.Value);

            await conn.OpenAsync();

            await cmd.ExecuteNonQueryAsync();
        }


        private string GenerateMockGroupCode()
        {
            return new string(Enumerable.Range(0, 4)
                .Select(_ => AlphaNumericChars[Random.Next(AlphaNumericChars.Length)])
                .ToArray());
        }

        public async Task<List<FlaggedBeneficiary>> GetAll(int tenantId)
        {
            var results = new List<FlaggedBeneficiary>();

            await using var conn = new SqlConnection(_connectionString);

            await conn.OpenAsync();

            await using var cmd = new SqlCommand(
                "dbo.sp_GetFlaggedBeneficiaries",
                conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@TenantId", SqlDbType.Int)
               .Value = tenantId;

            await using var reader = await cmd.ExecuteReaderAsync();

            int oId = reader.GetOrdinal("Id");
            int oBeneficiaryId = reader.GetOrdinal("BeneficiaryId");
            int oHouseholdId = reader.GetOrdinal("householdId");
            int oFullName = reader.GetOrdinal("FullName");
            int oActivityCode = reader.GetOrdinal("ActivityCode");
            int oDateFlagged = reader.GetOrdinal("DateFlagged");
            int oParticipationCode = reader.GetOrdinal("ParticipationCode");
            int oDownloadedOn = reader.GetOrdinal("DownloadedOn");
            int oDownloadedBy = reader.GetOrdinal("DownloadedBy");
            int oIsResolved = reader.GetOrdinal("IsResolved");
            int oResolvedOn = reader.GetOrdinal("ResolvedOn");

            while (await reader.ReadAsync())
            {
                results.Add(new FlaggedBeneficiary
                {
                    Id = reader.GetInt64(oId),

                    BeneficiaryId = reader.GetGuid(oBeneficiaryId)
                        .ToString(),
                    HouseholdId = reader.GetString(oHouseholdId)
                        .ToString(),

                    FullName = reader.IsDBNull(oFullName)
                        ? null
                        : reader.GetString(oFullName),

                    ActivityCode = reader.IsDBNull(oActivityCode)
                        ? null
                        : reader.GetString(oActivityCode),

                    DateFlagged = reader.GetDateTime(oDateFlagged),

                    ParticipationCode = reader.IsDBNull(oParticipationCode)
                        ? null
                        : reader.GetString(oParticipationCode),

                    DownloadedOn = reader.IsDBNull(oDownloadedOn)
                        ? (DateTime?)null
                        : reader.GetDateTime(oDownloadedOn),

                    DownloadedBy = reader.IsDBNull(oDownloadedBy)
                        ? null
                        : reader.GetString(oDownloadedBy),

                    IsResolved = !reader.IsDBNull(oIsResolved)
                        && reader.GetBoolean(oIsResolved),

                    ResolvedOn = reader.IsDBNull(oResolvedOn)
                        ? (DateTime?)null
                        : reader.GetDateTime(oResolvedOn)
                });
            }

            return results;
        }

        public async Task<string> Group(
            int tenantId,
            List<string> beneficiaryIds)
        {
            if (beneficiaryIds == null || beneficiaryIds.Count == 0)
            {
                throw new ArgumentException(
                    "No beneficiaries selected.");
            }

            string code = GenerateMockGroupCode();

            await using var conn = new SqlConnection(_connectionString);

            await conn.OpenAsync();

            await using var cmd = new SqlCommand(
                "dbo.sp_GroupFlaggedBeneficiaries",
                conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@ParticipationCode", SqlDbType.VarChar, 10)
               .Value = code;

            var table = new DataTable();

            table.Columns.Add("Value", typeof(string));

            beneficiaryIds.ForEach(id =>
            {
                table.Rows.Add(id);
            });

            var tvp = cmd.Parameters.AddWithValue(
                "@BeneficiaryIds",
                table);

            tvp.SqlDbType = SqlDbType.Structured;
            tvp.TypeName = "dbo.StringList";

            await cmd.ExecuteNonQueryAsync();

            return code;
        }

        public async Task AutoPartition(
         int tenantId,
         AutoPartitionParams autoPartition)
        {
            var beneficiaries =
                await GetUngroupedFlaggedBeneficiaries(tenantId);

            beneficiaries = AssignGroupsWhereNull(
                beneficiaries,
                autoPartition.Groups);

            await using var conn =
                new SqlConnection(_connectionString);

            await conn.OpenAsync();

            foreach (var beneficiary in beneficiaries
                .Where(x => !string.IsNullOrWhiteSpace(
                    x.ParticipationCode)))
            {
                await using var cmd = new SqlCommand(@"
            UPDATE dbo.tbl_FlaggedBeneficiary
            SET ParticipationCode = @ParticipationCode
            WHERE Id = @Id
        ", conn);

                cmd.Parameters.Add(
                    "@ParticipationCode",
                    SqlDbType.VarChar,
                    10)
                    .Value = beneficiary.ParticipationCode;

                cmd.Parameters.Add(
                    "@Id",
                    SqlDbType.BigInt)
                    .Value = beneficiary.Id;

                await cmd.ExecuteNonQueryAsync();
            }
        }

        public List<FlaggedBeneficiary> AssignGroupsWhereNull(
    List<FlaggedBeneficiary> beneficiaries,
    int totalGroups)
        {
            if (beneficiaries == null || beneficiaries.Count == 0)
                throw new ArgumentException("Beneficiaries list cannot be null or empty.");

            if (totalGroups <= 0)
                throw new ArgumentException("Total groups must be greater than zero.");

            // Select only beneficiaries with null ParticipationCode
            var ungrouped = beneficiaries
                .Where(b => b.ParticipationCode == null)
                .ToList();

            if (ungrouped.Count == 0)
                return beneficiaries; // Nothing to assign

            if (ungrouped.Count < totalGroups)
                throw new ArgumentException(
                    "Total groups cannot exceed the number of beneficiaries with null ParticipationCode.");

            // Shuffle to ensure random distribution
            var shuffled = ungrouped
                .OrderBy(_ => Guid.NewGuid())
                .ToList();

            // Generate distinct group codes
            var groupCodes = Enumerable
                .Range(0, totalGroups)
                .Select(_ => GenerateMockGroupCode())
                .ToList();

            // Assign codes evenly (round‑robin)
            for (int i = 0; i < shuffled.Count; i++)
            {
                int groupIndex = i % totalGroups;
                shuffled[i].ParticipationCode = groupCodes[groupIndex];
            }

            return beneficiaries;
        }



        private async Task<List<FlaggedBeneficiary>>
    GetUngroupedFlaggedBeneficiaries(int tenantId)
        {
            var results = new List<FlaggedBeneficiary>();

            await using var conn =
                new SqlConnection(_connectionString);

            await conn.OpenAsync();

            await using var cmd = new SqlCommand(@"
        SELECT
            FB.Id,
            FB.BeneficiaryId,
            FB.ActivityCode,
            FB.DateFlagged,
            FB.ParticipationCode,
            FB.IsResolved

        FROM dbo.tbl_FlaggedBeneficiary FB

        INNER JOIN dbo.tbl_Households H
            ON H.UUID = FB.BeneficiaryId

        WHERE
            (@TenantId = 0 OR H.TenantId = @TenantId)
            AND FB.ParticipationCode IS NULL
            AND FB.IsResolved = 0
    ", conn);

            cmd.Parameters.Add("@TenantId",
                SqlDbType.Int)
                .Value = tenantId;

            await using var reader =
                await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                results.Add(new FlaggedBeneficiary
                {
                    Id = reader.GetInt64(
                        reader.GetOrdinal("Id")),

                    BeneficiaryId = reader.GetGuid(
                        reader.GetOrdinal("BeneficiaryId"))
                        .ToString(),

                    ActivityCode = reader.IsDBNull(
                        reader.GetOrdinal("ActivityCode"))
                        ? null
                        : reader.GetString(
                            reader.GetOrdinal("ActivityCode")),

                    DateFlagged = reader.GetDateTime(
                        reader.GetOrdinal("DateFlagged")),

                    ParticipationCode = reader.IsDBNull(
                        reader.GetOrdinal("ParticipationCode"))
                        ? null
                        : reader.GetString(
                            reader.GetOrdinal("ParticipationCode")),

                    IsResolved = !reader.IsDBNull(
                        reader.GetOrdinal("IsResolved"))
                        && reader.GetBoolean(
                            reader.GetOrdinal("IsResolved"))
                });
            }

            return results;
        }


    }
}
