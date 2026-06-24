using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Interfaces.jobs;
using BRaVe_Management_Backend.Models;
using BRaVe_Management_Backend.DTOs;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Threading;

namespace BRaVe_Management_Backend.Services
{
    public class SqlDeduplicationJobService : IDeduplicationJobService
    {

        private readonly string _connectionString;
        private readonly ILogger<SqlDeduplicationJobService> _logger;

        public SqlDeduplicationJobService(ISecretProvider secretProvider, ILogger<SqlDeduplicationJobService> logger)
        {
            _connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection).Result;
            _logger = logger;

            _logger.LogInformation("SqlDeduplicationJobService initialized with connection string from KeyVault");
        }

        public async Task<long> CreateJobAsync(int TenantId, string UserId, DeduplicationJobRequest request, CancellationToken cancellationToken = default)
        {
            if (request.RuleId == null || request.StartPeriod == null || request.EndPeriod == null)
                throw new ArgumentException("RuleId, StartPeriod and EndPeriod are required for create.", nameof(request));

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);
            await using var cmd = new SqlCommand("sp_CreateDeduplicationJob", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@TenantId", TenantId);
            cmd.Parameters.AddWithValue("@RuleId", request.RuleId);
            cmd.Parameters.AddWithValue("@StartPeriod", request.StartPeriod.Date);
            cmd.Parameters.AddWithValue("@EndPeriod", request.EndPeriod.Date);
            //md.Parameters.AddWithValue("@StatusId", 1);
            cmd.Parameters.AddWithValue("@CreatedBy", UserId);

            var jobIdParam = new SqlParameter("@JobId", System.Data.SqlDbType.BigInt) { Direction = System.Data.ParameterDirection.Output };
            cmd.Parameters.Add(jobIdParam);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
            return Convert.ToInt32(jobIdParam.Value);
        }

        public async Task<DeduplicationJobDto?> GetJobByIdAsync(long jobId, CancellationToken cancellationToken = default)
        {
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync(cancellationToken);
                await using var cmd = new SqlCommand("sp_GetDeduplicationJob", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@JobId", jobId);
                await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                    return null;

                return MapToDto(reader);


            }
            catch (Exception e)
            {
                _logger.LogError(e, $"No job with JobId={jobId} was found");
                return null;
            }
            

        
        }

        public async Task<PagedDeduplicationJobsDto> GetJobsByTenantPagedAsync(int tenantId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            const string countSql = @"
            SELECT COUNT(1) FROM dbo.tbl_DeduplicationJobs WHERE TenantId = @TenantId";

            const string dataSql = @"
            SELECT j.JobId, j.TenantId, j.RuleId, j.StartPeriod, j.EndPeriod, j.StatusId, j.StartedOn, j.CreatedOn, j.CreatedBy, j.UpdatedOn, j.CompletedOn, j.ErrorMessage,
                   r.Name AS RulesetName,
                   s.Text AS StatusName
            FROM dbo.tbl_DeduplicationJobs j
            INNER JOIN dbo.tbl_DuplicateRulesets r ON j.RuleId = r.Id
            INNER JOIN dbo.lkp_TargetingStatus_Translations s ON j.StatusId = s.StatusId and s.LanguageCode='en'
            WHERE j.TenantId = @TenantId
            ORDER BY CASE WHEN j.StatusId = 2 THEN 0 ELSE 1 END, j.CreatedOn DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            int totalRecords;
            await using (var cmd = new SqlCommand(countSql, conn))
            {
                cmd.Parameters.AddWithValue("@TenantId", tenantId);
                var countResult = await cmd.ExecuteScalarAsync(cancellationToken);
                totalRecords = countResult != null && countResult != DBNull.Value ? Convert.ToInt32(countResult) : 0;
            }

            int totalPages = pageSize > 0 ? (int)Math.Ceiling(totalRecords / (double)pageSize) : 0;
            var jobs = new List<DeduplicationJobDto>();

            await using (var cmd = new SqlCommand(dataSql, conn))
            {
                cmd.Parameters.AddWithValue("@TenantId", tenantId);
                cmd.Parameters.AddWithValue("@Offset", (pageNumber - 1) * pageSize);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                    jobs.Add(MapToDto(reader));
            }

            return new PagedDeduplicationJobsDto(totalRecords, totalPages, jobs);
        }

        private static DeduplicationJobDto MapToDto(SqlDataReader reader)
        {
            return new DeduplicationJobDto
            {
                JobId = reader.GetInt64(reader.GetOrdinal("JobId")),
                TenantId = reader.GetInt32(reader.GetOrdinal("TenantId")),
                RuleId = reader.GetInt32(reader.GetOrdinal("RuleId")),
                RulesetName = reader.GetString(reader.GetOrdinal("RulesetName")),
                StartPeriod = reader.GetDateTime(reader.GetOrdinal("StartPeriod")),
                EndPeriod = reader.GetDateTime(reader.GetOrdinal("EndPeriod")),
                StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
                StatusName = reader.GetString(reader.GetOrdinal("StatusName")),
                CreatedOn = reader.GetDateTime(reader.GetOrdinal("CreatedOn")),
                UpdatedOn = reader.GetDateTime(reader.GetOrdinal("UpdatedOn")),
                CompletedOn = reader.IsDBNull(reader.GetOrdinal("CompletedOn")) ? null : reader.GetDateTime(reader.GetOrdinal("CompletedOn"))
            };
        }

        public async Task SetCompletedAsync(long jobId, CancellationToken cancellationToken)
        {
            const string sql = @"
            UPDATE tbl_DeduplicationJobs
            SET StatusId = @Completed, CompletedOn = GETDATE(), UpdatedOn = GETDATE()
            WHERE JobId = @Id";

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.Add(new SqlParameter("@Id", jobId));
            cmd.Parameters.Add(new SqlParameter("@Completed", TargetingJobStatus.Completed));

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task SetFailedAsync(long jobId, CancellationToken cancellationToken)
        {
            const string sql = @"
            UPDATE tbl_DeduplicationJobs
            SET StatusId = @Failed, CompletedOn = GETDATE(), UpdatedOn = GETDATE()
            WHERE JobId = @Id";

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.Add(new SqlParameter("@Id", jobId));
            cmd.Parameters.Add(new SqlParameter("@Failed", TargetingJobStatus.Failed));

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        public async void SaveDeduplicationResultsAsync(long jobId, int tenantId, List<DuplicateMatchResult> records, CancellationToken cancellationToken = default)
        {
            if (records == null || records.Count == 0)
                return;

            try
            {

                var table = ToDataTable(records);

                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                await using var cmd = connection.CreateCommand();
                cmd.CommandText = "sp_SaveDeduplicationResults";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@JobId", jobId));
                cmd.Parameters.Add(new SqlParameter("@TenantId", tenantId));
                cmd.Parameters.Add(new SqlParameter("@Results", SqlDbType.Structured)
                {
                    TypeName = "dbo.DeduplicationResultType",
                    Value = table
                });

                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error while saving deduplication result for jobId {jobId}");
                throw e;
            }
        }

        public static DataTable ToDataTable(List<DuplicateMatchResult> records)
        {
            var dt = new DataTable("tbl_DeduplicationResults");

            dt.Columns.Add("MemberUuidA", typeof(Guid));
            dt.Columns.Add("MemberIdA", typeof(string));
            dt.Columns.Add("DocumentIdA", typeof(string));
            dt.Columns.Add("FullnameA", typeof(string));
            dt.Columns.Add("AgeA", typeof(int));
            dt.Columns.Add("GenderA", typeof(string));
            dt.Columns.Add("PictureCollectedA", typeof(bool));
            dt.Columns.Add("BiometricCollectedA", typeof(bool));
            dt.Columns.Add("HouseholdIdA", typeof(string));
            dt.Columns.Add("RegisteredOnA", typeof(DateTime));
            dt.Columns.Add("MemberUuidB", typeof(Guid));
            dt.Columns.Add("MemberIdB", typeof(string));
            dt.Columns.Add("DocumentIdB", typeof(string));
            dt.Columns.Add("FullnameB", typeof(string));
            dt.Columns.Add("AgeB", typeof(int));
            dt.Columns.Add("GenderB", typeof(string));
            dt.Columns.Add("PictureCollectedB", typeof(bool));
            dt.Columns.Add("BiometricCollectedB", typeof(bool));
            dt.Columns.Add("HouseholdIdB", typeof(string));
            dt.Columns.Add("RegisteredOnB", typeof(DateTime));
            dt.Columns.Add("TotalScore", typeof(decimal));

            foreach (var r in records ?? Enumerable.Empty<DuplicateMatchResult>())
            {
                var row = dt.NewRow();

                row["MemberUuidA"] = r.MemberUuidA;
                row["MemberIdA"] = r.MemberIdA ?? string.Empty;
                row["DocumentIdA"] = string.IsNullOrEmpty(r.DocumentIdA) ? DBNull.Value : r.DocumentIdA;
                row["FullnameA"] = string.IsNullOrEmpty(r.FullnameA) ? DBNull.Value : r.FullnameA;
                row["AgeA"] = r.AgeA ?? (object)DBNull.Value;
                row["GenderA"] = string.IsNullOrEmpty(r.GenderA) ? DBNull.Value : r.GenderA;
                row["PictureCollectedA"] = r.PictureCollectedA;
                row["BiometricCollectedA"] = r.BiometricCollectedA;
                row["HouseholdIdA"] = string.IsNullOrEmpty(r.HouseholdIdA) ? DBNull.Value : r.HouseholdIdA;
                row["RegisteredOnA"] = r.RegisteredOnA ?? (object)DBNull.Value;

                row["MemberUuidB"] = r.MemberUuidB;
                row["MemberIdB"] = r.MemberIdB ?? string.Empty;
                row["DocumentIdB"] = string.IsNullOrEmpty(r.DocumentIdB) ? DBNull.Value : r.DocumentIdB;
                row["FullnameB"] = string.IsNullOrEmpty(r.FullnameB) ? DBNull.Value : r.FullnameB;
                row["AgeB"] = r.AgeB ?? (object)DBNull.Value;
                row["GenderB"] = string.IsNullOrEmpty(r.GenderB) ? DBNull.Value : r.GenderB;
                row["PictureCollectedB"] = r.PictureCollectedB;
                row["BiometricCollectedB"] = r.BiometricCollectedB;
                row["HouseholdIdB"] = string.IsNullOrEmpty(r.HouseholdIdB) ? DBNull.Value : r.HouseholdIdB;
                row["RegisteredOnB"] = r.RegisteredOnB ?? (object)DBNull.Value;
                row["TotalScore"] = (decimal)r.TotalScore;
                dt.Rows.Add(row);
            }

            return dt;
        }

        public async Task<DeduplicationJobDto?> ClaimNextQueuingJobAsync(CancellationToken cancellationToken)
        {
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync(cancellationToken);
                await using var cmd = new SqlCommand("sp_GetNextDeduplicationJob", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                
                await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                    return null;

                return MapToDto(reader);


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping deduplication job record");
                return null;
            }
        }

        public async Task<PagedResult<DuplicateMatchResult>> GetSavedResults(int TenantId,long JobId,int pageNumber,int pageSize,CancellationToken cancellationToken)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            await using var cmd = new SqlCommand("sp_GetSavedResults", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@TenantId", TenantId);
            cmd.Parameters.AddWithValue("@JobId", JobId);
            cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
            cmd.Parameters.AddWithValue("@PageSize", pageSize);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            var result = new PagedResult<DuplicateMatchResult>();

            /* TOTAL COUNT */
            if (await reader.ReadAsync(cancellationToken))
            {
                result.TotalCount = reader.GetInt32(0);
            }

            await reader.NextResultAsync(cancellationToken);

            /* DATA */
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Items.Add(MapToDuplicateMatchResult(reader));
            }

            return result;
        }

        private static DuplicateMatchResult MapToDuplicateMatchResult(SqlDataReader reader)
        {
            return new DuplicateMatchResult
            {
                MemberUuidA = reader.GetGuid(reader.GetOrdinal("MemberUuidA")),
                MemberIdA = reader.GetString(reader.GetOrdinal("MemberIdA")),
                DocumentIdA = reader.IsDBNull(reader.GetOrdinal("DocumentIdA")) ? string.Empty : reader.GetString(reader.GetOrdinal("DocumentIdA")),
                FullnameA = reader.IsDBNull(reader.GetOrdinal("FullnameA")) ? string.Empty : reader.GetString(reader.GetOrdinal("FullnameA")),
                AgeA = reader.IsDBNull(reader.GetOrdinal("AgeA")) ? null : reader.GetInt32(reader.GetOrdinal("AgeA")),
                GenderA = reader.IsDBNull(reader.GetOrdinal("GenderA")) ? string.Empty : reader.GetString(reader.GetOrdinal("GenderA")),
                PictureCollectedA = reader.GetBoolean(reader.GetOrdinal("PictureCollectedA")),
                BiometricCollectedA = reader.GetBoolean(reader.GetOrdinal("BiometricCollectedA")),
                HouseholdIdA = reader.IsDBNull(reader.GetOrdinal("HouseholdIdA")) ? string.Empty : reader.GetString(reader.GetOrdinal("HouseholdIdA")),
                RegisteredOnA = reader.IsDBNull(reader.GetOrdinal("RegisteredOnA")) ? null : reader.GetDateTime(reader.GetOrdinal("RegisteredOnA")),
                MemberUuidB = reader.GetGuid(reader.GetOrdinal("MemberUuidB")),
                MemberIdB = reader.GetString(reader.GetOrdinal("MemberIdB")),
                DocumentIdB = reader.IsDBNull(reader.GetOrdinal("DocumentIdB")) ? string.Empty : reader.GetString(reader.GetOrdinal("DocumentIdB")),
                FullnameB = reader.IsDBNull(reader.GetOrdinal("FullnameB")) ? string.Empty : reader.GetString(reader.GetOrdinal("FullnameB")),
                AgeB = reader.IsDBNull(reader.GetOrdinal("AgeB")) ? null : reader.GetInt32(reader.GetOrdinal("AgeB")),
                GenderB = reader.IsDBNull(reader.GetOrdinal("GenderB")) ? string.Empty : reader.GetString(reader.GetOrdinal("GenderB")),
                PictureCollectedB = reader.GetBoolean(reader.GetOrdinal("PictureCollectedB")),
                BiometricCollectedB = reader.GetBoolean(reader.GetOrdinal("BiometricCollectedB")),
                HouseholdIdB = reader.IsDBNull(reader.GetOrdinal("HouseholdIdB")) ? string.Empty : reader.GetString(reader.GetOrdinal("HouseholdIdB")),
                RegisteredOnB = reader.IsDBNull(reader.GetOrdinal("RegisteredOnB")) ? null : reader.GetDateTime(reader.GetOrdinal("RegisteredOnB")),
                TotalScore = (double)reader.GetDecimal(reader.GetOrdinal("TotalScore"))
            };
        }

        public async Task ClearDeduplicationResultsAsync(long jobId, int tenantId, string? userId, CancellationToken cancellationToken)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "sp_DisableCompletedDedupJob";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@JobId", SqlDbType.BigInt)
            {
                Value = jobId
            });

            cmd.Parameters.Add(new SqlParameter("@TenantId", SqlDbType.Int)
            {
                Value = tenantId
            });

            cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.VarChar, 50)
            {
                Value = userId
            });

            try
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (SqlException ex) when (ex.Number == 70001)
            {
                _logger.LogWarning(ex,
                    ex.Message,
                    jobId, tenantId, userId);


                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to disable targeting job {JobId} for tenant {TenantId} and user {UserId}",
                    jobId, tenantId, userId);


                throw;
            }
        }


    }
}
