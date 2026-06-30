using BRaVe_Management_Backend.DTOs;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IFileBasedDeduplicationService
    {
        Task<FileBasedDeduplicationProcessResult> ProcessUploadAsync(
            int TenantId,
        string json,
        string filename,
        string uploadedBy,
        CancellationToken cancellationToken = default);
    }
}
