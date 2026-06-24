using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IPmDecisionService
    {
        Task<Decision> GetCurrentDecision(int AssessmentId, int TenantId);
        Task SaveDecision(DecisionDto data, int TenantId, string UserId);
    }
}
