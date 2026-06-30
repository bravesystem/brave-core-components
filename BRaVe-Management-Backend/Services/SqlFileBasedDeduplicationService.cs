using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;

namespace BRaVe_Management_Backend.Services
{
    public class SqlFileBasedDeduplicationService : IFileBasedDeduplicationService
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlFailedBatchService> _logger;
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly IServiceScopeFactory _scopeFactory;

        //SqlFailedBatchService

        public SqlFileBasedDeduplicationService(ISecretProvider secretProvider,  ILogger<SqlFailedBatchService> logger)
        {
            _logger = logger;
            _connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection).Result;
        }

        public Task<FileBasedDeduplicationProcessResult> ProcessUploadAsync(int tenantId, string json, string filename, string uploadedBy, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
