using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Interfaces.jobs;
using BRaVe_Management_Backend.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BRaVe_Management_Backend.Services
{
    public class SqlTargetingJobService : ITargetingJob
    {

        private readonly string _connectionString;
        private readonly ILogger<SqlProgramService> _logger;

        public SqlTargetingJobService(ISecretProvider secretProvider, ILogger<SqlProgramService> logger)
        {
            _connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection).Result;
            _logger = logger;

            _logger.LogInformation("SqlTargetingJobService initialized with connection string from KeyVault");
        }


        public async Task<TargetingJob?> ClaimJobByIdAsync(long jobId, CancellationToken cancellationToken = default)
        {
            /*const string sql = @"
            UPDATE tbl_TargetingJobs
            SET StatusId = @Running, UpdatedOn = GETDATE(), StartedOn = GETDATE()
            OUTPUT INSERTED.JobId, INSERTED.TenantId, INSERTED.TargetingId, INSERTED.StartPeriod, INSERTED.EndPeriod,
                   INSERTED.CreatedBy, INSERTED.StatusId, INSERTED.CreatedOn, INSERTED.UpdatedOn, INSERTED.CompletedOn
            WHERE JobId = @Id AND StatusId = @Queuing;";*/

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "sp_TargetingJobs_SetRunning";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@Id", jobId));
            cmd.Parameters.Add(new SqlParameter("@Queuing", TargetingJobStatus.Queuing));
            cmd.Parameters.Add(new SqlParameter("@Running", TargetingJobStatus.Running));

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return null;

            return MapFromReader(reader);
        }

        private static TargetingJob MapFromReader(SqlDataReader reader)
        {
            return new TargetingJob
            {
                Id = reader.GetInt64(reader.GetOrdinal("JobId")),
                TenantId = reader.GetInt32(reader.GetOrdinal("TenantId")),
                TargetingId = reader.GetInt32(reader.GetOrdinal("TargetingId")),
                TargetingRule = reader.GetString(reader.GetOrdinal("TargetingRule")),
                StartPeriod = reader.GetDateTime(reader.GetOrdinal("StartPeriod")),
                EndPeriod = reader.GetDateTime(reader.GetOrdinal("EndPeriod")),
                CreatedBy = reader.GetString(reader.GetOrdinal("CreatedBy")),
                StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
                CreatedOn = reader.GetDateTime(reader.GetOrdinal("CreatedOn")),
                UpdatedOn = reader.GetDateTime(reader.GetOrdinal("UpdatedOn")),
                CompletedOn = reader.IsDBNull(reader.GetOrdinal("CompletedOn")) ? null : reader.GetDateTime(reader.GetOrdinal("CompletedOn"))
            };
        }

        public async Task<TargetingJob?> ClaimNextQueuingJobAsync(CancellationToken cancellationToken = default)
        {
           throw new NotImplementedException();
        }

        public async Task<int> CreateJobAsync(int TenantId, string UserId,TargetingJobRequest request, CancellationToken cancellationToken = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "sp_CreateTargetingJob";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("@TenantId", TenantId));
            cmd.Parameters.Add(new SqlParameter("@TargetingId", request.TargetingId));
            cmd.Parameters.Add(new SqlParameter("@StartPeriod", request.StartPeriod));
            cmd.Parameters.Add(new SqlParameter("@EndPeriod", request.EndPeriod));
            cmd.Parameters.Add(new SqlParameter("@CreatedBy", UserId));

            var jobIdParam = new SqlParameter("@JobId", System.Data.SqlDbType.Int) { Direction = System.Data.ParameterDirection.Output };
            cmd.Parameters.Add(jobIdParam);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
            return Convert.ToInt32(jobIdParam.Value);
        }

        public async Task SetCompletedAsync(long jobId, CancellationToken cancellationToken = default)
        {
            const string sql = @"
            UPDATE tbl_TargetingJobs
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

        public async Task SetFailedAsync(long jobId, CancellationToken cancellationToken = default)
        {
            const string sql = @"
            UPDATE tbl_TargetingJobs
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

        public async Task<TargetingJob?> GetJobByIdAsync(long jobId, CancellationToken cancellationToken = default)
        {
            const string sql = @"
            SELECT JobId, t.TenantId, TargetingId,r.RuleName TargetingRule, StartPeriod, EndPeriod, CreatedBy,
                   StatusId, t.CreatedOn, UpdatedOn, CompletedOn
            FROM tbl_TargetingJobs t
            JOIN dbo.tbl_TargetingRules r on t.TargetingId=r.Id
            WHERE JobId = @Id";

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.Add(new SqlParameter("@Id", jobId));

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return null;

            return MapFromReader(reader);
        }

        public async Task<PagedTargetingJobsResult> GetJobsByTenantPagedAsync(int tenantId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "sp_GetTargetingJobsByTenantPaged";
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("@TenantId", tenantId));
            cmd.Parameters.Add(new SqlParameter("@PageNumber", pageNumber));
            cmd.Parameters.Add(new SqlParameter("@PageSize", pageSize));

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            var totalRecords = 0;
            var totalPages = 0;

            if (await reader.ReadAsync(cancellationToken))
            {
                totalRecords = reader.GetInt32(reader.GetOrdinal("TotalRecords"));
                totalPages = reader.GetInt32(reader.GetOrdinal("TotalPages"));
            }

            var jobs = new List<TargetingJob>();
            if (await reader.NextResultAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    jobs.Add(MapFromReader(reader));
                }
            }

            var sortedJobs = jobs
                .OrderBy(j => j.Order)           // Running first
                .ThenByDescending(j => j.CreatedOn) // Newest next
                .ToList();

            return new PagedTargetingJobsResult(totalRecords, totalPages, sortedJobs);

        }

        public async Task SaveTargetingResultsAsync(long jobId, int tenantId, List<ScoredResult> scoredResults, CancellationToken cancellationToken = default)
        {
            if (scoredResults == null || scoredResults.Count == 0)
                return;

            var table = new DataTable();
            table.Columns.Add("DocumentId", typeof(string));
            table.Columns.Add("Score", typeof(decimal));
            table.Columns.Add("Rank", typeof(int));
            table.Columns.Add("DenseRank", typeof(int));
            table.Columns.Add("RowNumber", typeof(int));

            foreach (var r in scoredResults)
            {
                table.Rows.Add(
                    r.DocumentId ?? "",
                    (decimal)r.Score,
                    r.Rank,
                    r.DenseRank,
                    r.RowNumber);
            }

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "sp_SaveTargetingResults";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("@JobId", jobId));
            cmd.Parameters.Add(new SqlParameter("@TenantId", tenantId));
            cmd.Parameters.Add(new SqlParameter("@Results", SqlDbType.Structured)
            {
                TypeName = "dbo.ScoredResultType",
                Value = table
            });

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task ClearTargetingResultsAsync(long jobId, int tenantId,string userId,CancellationToken cancellationToken = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "sp_DisableCompletedJob";
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
            catch (SqlException ex) when (ex.Number==70001)
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
