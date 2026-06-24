using BRaVe_Management_Backend.DTOs;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface ISqlBiometricService
    {

        //to support the adjudication process
        Task<Guid> RemoveBiometricMatchingRequest(int tenantId, Guid uuid);
        Task<Guid> ReprocessBiometricMatchingRequest(int tenantId, Guid uuid);
        //

        Task<PagedResult<BiometricVerificationResultDto>> GetBiometricVerificationsAsync( int tenantId, int pageNumber,int pageSize);

        Task<List<DuplicateIndicatorChecklistDto>>GetDuplicateIndicatorChecklistAsync(int? tenantId, string languageCode);

        Task<List<ProgrammaticDataDto>> GetProgrammaticDataAsync(int tenantId,string householdId);

        Task<PagedResult<BiometricMatchListDto>>GetBiometricMatchListAsync(int tenantId,int pageNumber,int pageSize,string? activity,DateTime? dateFrom, DateTime? dateTo, int? minScore);

        Task<BiometricMatchResultDto?> GetBiometricMatchDetailsAsync(int tenantId,Guid sourceUuid,Guid matchedUuid);

        Task<List<string>> GetAvailableMatchActivitiesAsync(int tenantId);

        Task<BiometricMatchResultDto?> GetManualMatchDetailsAsync(int tenantId,Guid sourceUuid,Guid matchedUuid);


    }
}
