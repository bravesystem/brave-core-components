using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IRiskBenefitAssessment
    {
        Task<RiskBenefitAssessmentMetadataDto> GetAssessmentById(int AssessmentId, int TenantId, string Language);
        Task<List<RiskBenefitAssessment>> GetAllAssessments(int TenantId, string Language);
        Task<RiskBenefitAssessment> CreateRiskBenefitAssessment(RiskBenefitAssessmentDto data, string UserId);
    }
}
