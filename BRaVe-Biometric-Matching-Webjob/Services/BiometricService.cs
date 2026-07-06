using Azure.Core;
using BRaVe_Biometric_Matching_Webjob.Extensions;
using BRaVe_Biometric_Matching_Webjob.Helpers;
using BRaVe_Biometric_Matching_Webjob.Interfaces;
using BRaVe_Biometric_Matching_Webjob.Models;
using Neurotec.Biometrics;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace BRaVe_Biometric_Matching_Webjob.Services
{
    public class BiometricService : IBiometricService
    {
        private readonly string connectionString;

        public BiometricService(ISecretProvider secrerProvider)
        {
            //connectionString = Environment.GetEnvironmentVariable(KeyVaultSecretNames.Sql.PrimaryConnection);

            connectionString = secrerProvider.GetSecret(KeyVaultSecretNames.Sql.PrimaryConnection);
        }

        public async Task<List<Biometric>> GetBiometrics(string jobId, CancellationToken ct)
        {
            var biometrics = new List<Biometric>();

            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand("sp_BiometricMatchingRequests", conn))
            //using (var cmd = new SqlCommand("sp_BiometricMatchingRequests", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                Guid uuid = new Guid(jobId);

                cmd.Parameters.Add(new SqlParameter("@JobId", SqlDbType.UniqueIdentifier) { Value = uuid });

                await conn.OpenAsync();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {

                        var biometric = new Biometric
                        {
                            Id = reader["Id"].ToString(),
                            TenantId = int.Parse(reader["TenantId"].ToString()),
                            Template = reader["Template"] as byte[],
                            Gender = int.Parse(reader["Gender"].ToString()),
                            MatchingAction = reader["MatchingAction"].ToString(),
                            IsEncrypted = (bool)reader["IsEncrypted"]
                        };

                        biometrics.Add(biometric);
                    }
                }
            }

            return biometrics;

        }

        public async Task LogUnknownAllStatuses(List<BiometricMatch> biometricMatches)
        {
            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand("sp_LogUnknownAllStatuses", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                int TenantId = biometricMatches.FirstOrDefault().TenantId;

                DataTable dt =
                    biometricMatches.Select(x => new ProcessedSubject
                    {
                        Id = new Guid(x.Id),
                        Status = (int)x.Status
                    })
                    .ToList()
                    .ToDataTable();

                cmd.Parameters.Add(new SqlParameter("@TenantId", SqlDbType.Int) { Value = TenantId });

                SqlParameter sqlParameter = cmd.Parameters.AddWithValue("@UuidStatuses", dt);
                sqlParameter.SqlDbType = SqlDbType.Structured;

                await conn.OpenAsync();

                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task UpdateMatchStatuses(List<BiometricMatch> biometricMatches, NBiometricStatus status)
        {
            switch (status)
            {
                case NBiometricStatus.Canceled:
                    SetBiometricStatuses("sp_BiometricMarkAsReplaced", biometricMatches);
                    break;
                case NBiometricStatus.DuplicateId: //602
                case NBiometricStatus.DuplicateFound: //611
                case NBiometricStatus.TooFewFeatures: //49
                    MarkAsDuplicates((int)status, biometricMatches);
                    break;
                case NBiometricStatus.MatchNotFound:
                    ;
                    SetBiometricStatuses("sp_BiometricMarkAsEnrolled", biometricMatches);
                    break;
                case NBiometricStatus.None:
                default:
                    break;
            }
        }

        private async void SetBiometricStatuses(string procedure, List<BiometricMatch> biometricMatches)
        {
            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(procedure, conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                int TenantId = biometricMatches.FirstOrDefault().TenantId;

                DataTable dt =
                    biometricMatches.Select(x => new Guid(x.Id))
                        .ToList()
                        .ToDataTable();

                cmd.Parameters.Add(new SqlParameter("@TenantId", SqlDbType.Int) { Value = TenantId });

                SqlParameter sqlParameter = cmd.Parameters.AddWithValue("@UuidList", dt);
                sqlParameter.SqlDbType = SqlDbType.Structured;

                await conn.OpenAsync();

                await cmd.ExecuteNonQueryAsync();
            }
        }

        private async void MarkAsDuplicates(int status, List<BiometricMatch> biometricMatches)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand("sp_BiometricMarkAsDuplicates", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    int TenantId = biometricMatches.FirstOrDefault().TenantId;

                    DataTable dt = biometricMatches.Select(x => x.GetStaging())
                            .ToList()
                            .ToDataTable();

                    cmd.Parameters.Add(new SqlParameter("@TenantId", SqlDbType.Int) { Value = TenantId });

                    SqlParameter sqlParameter = cmd.Parameters.AddWithValue("@MatchedTemplates", dt);
                    sqlParameter.SqlDbType = SqlDbType.Structured;

                    await conn.OpenAsync();

                    await cmd.ExecuteNonQueryAsync();
                }

            }
            catch (Exception e)
            {

            }

            return;

        }

        
    }
}
