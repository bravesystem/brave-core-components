using BRaVe_Mobile_Backend.DTOs;

namespace BRaVe_Mobile_Backend.Interfaces
{
    public interface IClaimService
    {
        public sealed record ClaimResult(string HouseholdPrefix, int InitialId, string EnrollmentJws, string RefreshToken);
        public sealed record SessionRecord(long SessionId, int TenantId, int PolicyVersion, int MaxClaims, int ClaimsIssued, DateTime ExpiresAtUtc);

        Task<ClaimResult> ClaimAsync(ClaimRequest req, string? ip);

        Task<SessionRecord?> GetActiveSessionAsync(string deviceId, byte[] sessionCodeHash);

       // Task<Guid> InsertClaimAndConsumeAsync(long sessionId, int tenantId, byte[] thumb, string jti, DateTime jwsExpUtc, string? ip);
    }
}
