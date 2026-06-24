using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Interfaces.jobs;
using BRaVe_Management_Backend.Models;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface ITargetingJob
    {
        Task<int> CreateJobAsync(int TenantId, string UserId, TargetingJobRequest request, CancellationToken cancellationToken = default);
        Task<PagedTargetingJobsResult> GetJobsByTenantPagedAsync(int tenantId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<TargetingJob?> GetJobByIdAsync(long jobId, CancellationToken cancellationToken = default);
        Task<TargetingJob?> ClaimJobByIdAsync(long jobId, CancellationToken cancellationToken = default);

        Task<TargetingJob?> ClaimNextQueuingJobAsync(CancellationToken cancellationToken = default);

        Task SetCompletedAsync(long jobId, CancellationToken cancellationToken = default);

        Task SetFailedAsync(long jobId, CancellationToken cancellationToken = default);
        Task SaveTargetingResultsAsync(long jobId, int tenantId, List<ScoredResult> scoredResults, CancellationToken cancellationToken = default);

        Task ClearTargetingResultsAsync(long jobId, int tenantId, string userId, CancellationToken cancellationToken = default);


    }

}
