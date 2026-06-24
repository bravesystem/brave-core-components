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
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

using System.Threading.Tasks;

namespace BRaVe_Management_Backend.Services
{
    public class SqlFailedBatchService : IMonitoringService
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlFailedBatchService> _logger;
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly IServiceScopeFactory _scopeFactory;

        public SqlFailedBatchService(ISecretProvider secretProvider, IBackgroundTaskQueue taskQueue, IServiceScopeFactory scopeFactory, ILogger<SqlFailedBatchService> logger)
        {
            _logger = logger;
            _taskQueue = taskQueue;
            _scopeFactory = scopeFactory;
            _connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection).Result;
        }

        public async Task<List<FailedJobGridRowDto>> GetFailedBatchesAsync(int? tenantId)
        {
            var result = new List<FailedJobGridRowDto>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("dbo.sp_GetFailedBatches", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@TenantId",
                tenantId.HasValue ? tenantId.Value : DBNull.Value);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new FailedJobGridRowDto
                {
                    BatchId = reader.GetGuid("BatchId"),
                    Mission = reader.GetString("Mission"),
                    SyncAttemptOn = reader.GetDateTime("SyncAttemptOn"),
                    DeviceId = reader.GetString("DeviceId")
                });
            }

            return result;
        }


        public async Task HandleAsync(Guid jobId)
        {
            _logger.LogInformation("Starting bg job service to Load for job {JobId}", jobId);

            _taskQueue.QueueBackgroundWorkItem(async ct =>
            {
                try
                {
                    _logger.LogInformation("Injecting IDataLoaderService service");

                    using var scope = _scopeFactory.CreateScope();
                    var loader = scope.ServiceProvider.GetRequiredService<IDataLoaderService>();

                    _logger.LogInformation("Loading job {JobId}", jobId);

                    await loader.LoadAsync(jobId);// sync call, wrapped in async work item
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error running IDataLoaderService. Load for job {JobId}", jobId);
                }

                await Task.CompletedTask;
            });
        }
    }
}
