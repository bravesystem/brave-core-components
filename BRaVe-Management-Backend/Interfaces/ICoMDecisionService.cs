using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface ICoMDecisionService
    {
        Task<Decision> GetCurrentDecision(int AssessmentId, int TenantId);
        Task SaveDecision(DecisionDto data, int TenantId,string UserId);
        //Task UpdateDecision(CoMDecisionDto data, int TenantId,string UserId);
    }
}
