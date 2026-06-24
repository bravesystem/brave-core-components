using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IDuplicateRulesetService
    {
        Task<List<DuplicateRulesetViewModel>> GetAll(int TenantId);
        Task<DuplicatePredicateViewModel> GetPredicates(int TenantId, int RulesetId);
        Task<DuplicateCriteriaDefinition> GetCriteriaDefinitionAsync(int TenantId, int rulesetId);
        Task<PredicateEvaluationViewModel> CompareIndividuals(int tenantId, IndividualPairDto dto);
    }
}
