using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;

namespace BRaVe_Management_Backend.Services
{

    public class SqlBeneficiaryEnrollmentsService : IBeneficiaryEnrollmentsService
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlBeneficiaryEnrollmentsService> _logger;

        public SqlBeneficiaryEnrollmentsService(ISecretProvider secretProvider, ILogger<SqlBeneficiaryEnrollmentsService> logger)
        {
            _logger = logger;
            _connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection)
                                             .ConfigureAwait(false)
                                             .GetAwaiter()
                                             .GetResult();
        }
        public async Task<IEnumerable<DistributionEnrollmentDto>> GetBeneficiaryEnrollmentsAsync(
    int tenantId,
    int distributionId)
        {
            var results = new List<DistributionEnrollmentDto>();

            try
            {
                using var conn = new SqlConnection(_connectionString);

                using var cmd = new SqlCommand("dbo.sp_GetDistributionEnrollmentsV2", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.Add("@TenantId", SqlDbType.Int).Value = tenantId;
                cmd.Parameters.Add("@DistributionId", SqlDbType.Int).Value = distributionId;

                await conn.OpenAsync();

                using var reader = await cmd.ExecuteReaderAsync();

                if (!reader.HasRows)
                {
                    _logger.LogWarning(
                        "No enrollments found for Tenant {TenantId}, Distribution {DistributionId}",
                        tenantId,
                        distributionId);
                }

                while (await reader.ReadAsync())
                {
                    results.Add(new DistributionEnrollmentDto
                    {
                        HouseholdId = reader["householdId"]?.ToString(),

                        IndividualId = reader["individualId"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(reader["individualId"]),

                        Gender = reader["gender"]?.ToString(),


                        FullName = reader["FullName"]?.ToString(),

                        Age = reader["Age"] == DBNull.Value
                            ? null
                            : Convert.ToInt32(reader["Age"]),

                        Location = reader["Location"]?.ToString(),

                        Distributed = reader["Distributed"] != DBNull.Value &&
                                      Convert.ToInt32(reader["Distributed"]) == 1,

                        ReceivedOn = reader["ReceivedOn"] == DBNull.Value
                            ? null
                            : Convert.ToDateTime(reader["ReceivedOn"]),
                        ReceivedAll = reader["ReceivedAll"] != DBNull.Value &&
                                      Convert.ToBoolean(reader["ReceivedAll"]),

                        Distribution = reader["Distribution"]?.ToString(),

                        ////DistributionKit = reader["DistributionKit"]?.ToString(),

                        DistributionId = reader["DistributionId"] != DBNull.Value
                                        ? Convert.ToInt32(reader["DistributionId"])
                                        : 0,


                        EnrolledOn = reader["EnrolledOn"] == DBNull.Value
                            ? DateTime.MinValue
                            : Convert.ToDateTime(reader["EnrolledOn"]),

                        TargetingUsed = reader["TargetingUsed"]?.ToString(),

                        biometricConfirmation = reader["biometricConfirmation"] != DBNull.Value &&
                                      Convert.ToBoolean(reader["biometricConfirmation"]),

                        photoConfirmation = reader["photoConfirmation"] != DBNull.Value &&
                                      Convert.ToBoolean(reader["photoConfirmation"]),


                        photoBase64 = reader["photoBase64"]?.ToString(),

                        data = reader["ItemsReceived"] == DBNull.Value
                        ? new Dictionary<string, bool>()
                        : JsonSerializer.Deserialize<Dictionary<string, bool>>(reader["ItemsReceived"].ToString()),

                        Score = reader["Score"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["Score"]),

                        Rank = reader["Rank"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(reader["Rank"]),

                        DenseRank = reader["DenseRank"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(reader["DenseRank"]),

                        RowNumber = reader["RowNumber"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(reader["RowNumber"])
                    });
                }

                _logger.LogInformation(
                    "Loaded {Count} enrollments for Distribution {DistributionId}",
                    results.Count,
                    distributionId);

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error fetching enrollments for Tenant {TenantId}, Distribution {DistributionId}",
                    tenantId,
                    distributionId);

                throw; // IMPORTANT: let controller see error
            }
        }


        public async Task<IEnumerable<DistributionEnrollmentDto>> GetBeneficiaryAllEnrollmentsAsync(
int tenantId)
        {
            var results = new List<DistributionEnrollmentDto>();

            try
            {
                using var conn = new SqlConnection(_connectionString);

                using var cmd = new SqlCommand("dbo.sp_GetDistributionAllEnrollmentsV2", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.Add("@TenantId", SqlDbType.Int).Value = tenantId;


                await conn.OpenAsync();

                using var reader = await cmd.ExecuteReaderAsync();

                if (!reader.HasRows)
                {
                    _logger.LogWarning(
                        "No enrollments found for Tenant {TenantId}, Distribution {DistributionId}",
                        tenantId);
                }

                while (await reader.ReadAsync())
                {
                    results.Add(new DistributionEnrollmentDto
                    {
                        HouseholdId = reader["householdId"]?.ToString(),

                        IndividualId = reader["individualId"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(reader["individualId"]),

                        Gender = reader["gender"]?.ToString(),

                        FullName = reader["FullName"]?.ToString(),

                        Age = reader["Age"] == DBNull.Value
                            ? null
                            : Convert.ToInt32(reader["Age"]),

                        Location = reader["Location"]?.ToString(),

                        Distributed = reader["Distributed"] != DBNull.Value &&
                                      Convert.ToInt32(reader["Distributed"]) == 1,

                        ReceivedOn = reader["ReceivedOn"] == DBNull.Value
                            ? null
                            : Convert.ToDateTime(reader["ReceivedOn"]),

                        ReceivedAll = reader["ReceivedAll"] != DBNull.Value &&
                                      Convert.ToBoolean(reader["ReceivedAll"]),

                        Distribution = reader["Distribution"]?.ToString(),
                        DistributionId = reader["DistributionId"] != DBNull.Value
                                        ? Convert.ToInt32(reader["DistributionId"])
                                        : 0,

                        EnrolledOn = reader["EnrolledOn"] == DBNull.Value
                            ? DateTime.MinValue
                            : Convert.ToDateTime(reader["EnrolledOn"]),

                        TargetingUsed = reader["TargetingUsed"]?.ToString(),

                        biometricConfirmation = reader["biometricConfirmation"] != DBNull.Value &&
                                      Convert.ToBoolean(reader["biometricConfirmation"]),

                        photoConfirmation = reader["photoConfirmation"] != DBNull.Value &&
                                      Convert.ToBoolean(reader["photoConfirmation"]),


                        photoBase64 = reader["photoBase64"]?.ToString(),

                        data = reader["ItemsReceived"] == DBNull.Value
                        ? new Dictionary<string, bool>()
                        : JsonSerializer.Deserialize<Dictionary<string, bool>>(reader["ItemsReceived"].ToString()),

                        Score = reader["Score"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["Score"]),

                        Rank = reader["Rank"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(reader["Rank"]),

                        DenseRank = reader["DenseRank"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(reader["DenseRank"]),

                        RowNumber = reader["RowNumber"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(reader["RowNumber"])
                    });
                }

                _logger.LogInformation(
                    "Loaded {Count} enrollments for all distributions",
                    results.Count);

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error fetching enrollments for Tenant {TenantId}",
                    tenantId);

                throw; 
            }
        }


        public async Task<DistributionEnrollmentDto?> GetEnrollmentByIdsAsync(
    int tenantId,
    int distributionId,
    int individualId, string householdId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);

                using var cmd = new SqlCommand("dbo.sp_GetDistributionEnrollmentByIdsV2", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.Add("@TenantId", SqlDbType.Int).Value = tenantId;
                cmd.Parameters.Add("@DistributionId", SqlDbType.Int).Value = distributionId;
                cmd.Parameters.Add("@IndividualId", SqlDbType.Int).Value = individualId;
                cmd.Parameters.Add("@HouseholdId", SqlDbType.NVarChar, 50).Value = householdId;


                await conn.OpenAsync();

                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    _logger.LogWarning(
                        "No enrollment found for Tenant {TenantId}, Distribution {DistributionId}, Individual {IndividualId}",
                        tenantId,
                        distributionId,
                        individualId);

                    return null;
                }

                var result = new DistributionEnrollmentDto
                {
                    HouseholdId = reader["householdId"]?.ToString(),

                    IndividualId = reader["individualId"] == DBNull.Value
                        ? 0
                        : Convert.ToInt32(reader["individualId"]),

                    Gender = reader["gender"]?.ToString(),

                    FullName = reader["FullName"]?.ToString(),

                    Age = reader["Age"] == DBNull.Value
                        ? null
                        : Convert.ToInt32(reader["Age"]),

                    Location = reader["Location"]?.ToString(),

                    Distributed = reader["Distributed"] != DBNull.Value &&
                                  Convert.ToInt32(reader["Distributed"]) == 1,

                    ReceivedOn = reader["ReceivedOn"] == DBNull.Value
                        ? null
                        : Convert.ToDateTime(reader["ReceivedOn"]),
                     ReceivedAll = reader["ReceivedAll"] != DBNull.Value &&
                                      Convert.ToBoolean(reader["ReceivedAll"]),

                    Distribution = reader["Distribution"]?.ToString(),

                    DistributionId = reader["DistributionId"] != DBNull.Value
                        ? Convert.ToInt32(reader["DistributionId"])
                        : 0,

                    EnrolledOn = reader["EnrolledOn"] == DBNull.Value
                        ? DateTime.MinValue
                        : Convert.ToDateTime(reader["EnrolledOn"]),

                    TargetingUsed = reader["TargetingUsed"]?.ToString(),

                    biometricConfirmation = reader["BiometricConfirmation"] != DBNull.Value &&
                                            Convert.ToBoolean(reader["BiometricConfirmation"]),

                    photoConfirmation = reader["PhotoConfirmation"] != DBNull.Value &&
                                        Convert.ToBoolean(reader["PhotoConfirmation"]),

                    photoBase64 = reader["PhotoBase64"]?.ToString(),
                    comment = reader["Comment"] == DBNull.Value
                                ? null
                                : reader["Comment"].ToString(),
                    

                    data = reader["ItemsReceived"] == DBNull.Value
                        ? new Dictionary<string, bool>()
                        : JsonSerializer.Deserialize<Dictionary<string, bool>>(
                            reader["ItemsReceived"].ToString()),

                    Score = reader["Score"] == DBNull.Value
                        ? 0
                        : Convert.ToDecimal(reader["Score"])
                };

                _logger.LogInformation(
                    "Loaded enrollment for Distribution {DistributionId}, Individual {IndividualId}",
                    distributionId,
                    individualId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error fetching enrollment for Tenant {TenantId}, Distribution {DistributionId}, Individual {IndividualId}",
                    tenantId,
                    distributionId,
                    individualId);

                throw;
            }
        }


    }
}