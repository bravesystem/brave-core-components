using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;
using System.Text.Json;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface ITargetingRulesService
    {
        // CREATE
        Task<int> CreateAsync(TargetingRuleDto dto);

        // READ
        Task<IEnumerable<TargetingRuleDto>> GetAllAsync(int TenantId);
        Task<TargetingRuleDto?> GetByIdAsync(int ruleId);

        // UPDATE
        Task UpdateAsync(int ruleId, TargetingRuleDto dto);

        // DELETE
        Task DeleteAsync(int ruleId);

        Task<List<TargetingFieldsDto>> GetAllTargetingFieldsAsync(int tenantId);

        Task<IEnumerable<string>> GetDistinctValuesForFieldAsync(string displayName);
        Task<IEnumerable<TargetingPreviewDto>> PreviewAsync(JsonElement ruleJson);

        Task EnrollBeneficiariesAsync(EnrollBeneficiariesDto dto);

    }
}
