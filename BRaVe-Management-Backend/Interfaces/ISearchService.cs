using BRaVe_Management_Backend.Models;
using BRaVe_Management_Backend.Models.es;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface ISearchService
    {
        Task<(Household_Document, Household_Document)> GetAllHouseholdPair(int TenantId, string householdIdA, string householdIdB);

        Task<List<Household_Document>> GetAllHouseholds(int TenantId, DateTime from, DateTime to);

        //Task<List<Household_Document>> GetAllHouseholds(object tenantId, object startPeriod, object endPeriod);
        Task<RuleDefinition> GetRuleDefinition(int targetingId);
        //Task<List<Household_Document>> GetAllHouseholds(int TenantId, List<string> householdIds);

        Task<List<TargetingResultDto>> GetSavedScorings(int TenantId, long JobId);

        //Task<List<DuplicateMatchResultsViewModel>> GetSavedDedupResults(int TenantId, long JobId);

    }
}
