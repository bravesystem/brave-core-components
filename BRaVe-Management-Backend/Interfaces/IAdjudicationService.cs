using BRaVe_Management_Backend.DTOs;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IAdjudicationService
    {
        Task<List<AdjudicationDecisionDto>> GetAdjudicationDecisionsAsync(CancellationToken cancellationToken = default);

        Task<AdjudicationResultDto> AdjudicateAsync(AdjudicationRequestDto request,CancellationToken cancellationToken = default);

        Task BulkAdjudicateAsync(List<AdjudicationRequestDto> requests,CancellationToken cancellationToken = default);
        Task<List<MemberMatchHistoryDto>> GetMemberMatchHistoryAsync(Guid selectedUuid,CancellationToken cancellationToken = default);

        Task<PagedResultDto<AdjudicationHistoryDto>>
    GetAdjudicationHistoryAsync(
        int tenantId,
        int pageNumber,
        int pageSize,
        int? decisionId = null,
        string? dedupMode = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string? adjudicatedBy = null,
        CancellationToken cancellationToken = default);
    }
}
