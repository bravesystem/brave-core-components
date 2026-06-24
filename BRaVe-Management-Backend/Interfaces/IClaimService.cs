using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.DTOs.claim_session;
using static BRaVe_Management_Backend.Services.SqlClaimService;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IClaimService
    {
        public sealed record ClaimResult(string EnrollmentJws);
        public sealed record SessionRecord(long SessionId, int TenantId, int PolicyVersion, int MaxClaims, int ClaimsIssued, DateTime ExpiresAtUtc);

        Task<long> CreateAsync(ClaimSessionRecord record);

        Task<ClaimResult> ClaimAsync(ClaimRequest req, string? ip);

        Task<SessionRecord?> GetActiveSessionAsync(byte[] sessionCodeHash);

        Task<Guid> InsertClaimAndConsumeAsync(long sessionId, int tenantId, byte[] thumb, string jti, DateTime jwsExpUtc, string? ip);

        Task<long> CreatePrintSessionAsync(PrintSessionRecordCreate record);

        Task<PrintSessionRecord?> GetActivePrintSessionAsync(byte[] sessionCodeHash);

        Task<PrintClaimResult> ClaimPrintAsync(string sessionCode, string deviceId);

        Task<List<PrintSessionDto>> GetPrintSessionsAsync(int tenantId);

        Task RevokePrintSessionAsync(long sessionId, int tenantId);
    }
}
