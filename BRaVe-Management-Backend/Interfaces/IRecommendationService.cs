using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IRecommendationService
    {
        Task<Recommendation> GetCurrentRecommendation(int AssessmentId, bool IsLeg, int TenantId);
        Task SaveRecommendation(RecommendationDto data, int TenantId, string UserId);
    }
}
