using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Interfaces.jobs;
using BRaVe_Management_Backend.Models;
using DocumentFormat.OpenXml.Wordprocessing;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IDeduplicationJobService
    {
        Task<long> CreateJobAsync(int TenantId, string UserId, DeduplicationJobRequest request, CancellationToken cancellationToken = default);
        Task<PagedDeduplicationJobsDto> GetJobsByTenantPagedAsync(int tenantId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<DeduplicationJobDto?> GetJobByIdAsync(long jobId, CancellationToken cancellationToken = default);
        Task SetCompletedAsync(long jobId, CancellationToken stoppingToken);
        Task SetFailedAsync(long jobId, CancellationToken stoppingToken);
        Task<PagedResult<DuplicateMatchResult>> GetSavedResults(int TenantId,long JobId,int pageNumber,int pageSize,CancellationToken cancellationToken);
        void SaveDeduplicationResultsAsync(long jobId, int tenantId, List<DuplicateMatchResult> records, CancellationToken cancellationToken = default);
        Task<DeduplicationJobDto?> ClaimNextQueuingJobAsync(CancellationToken stoppingToken);
        Task ClearDeduplicationResultsAsync(long jobId, int tenantId, string? userId, CancellationToken cancellationToken);
    }
}
