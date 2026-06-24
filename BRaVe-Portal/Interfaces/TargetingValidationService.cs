using BRaVe_Portal.Models.DTOs;

namespace BRaVe_Portal.Interfaces
{
    public class TargetingValidationService
    {
        private readonly IRestApiService _api;

        public TargetingValidationService(IRestApiService api)
        {
            _api = api;
        }

        public async Task<(bool isValid, decimal totalWeight, bool hasCriteria)> ValidateRuleAsync(
            int ruleId,
            decimal ruleWeight,
            bool weightBound)
        {
            var allCriteria = await _api.GetAsync<List<TargetingCriteriaDto>>(
                $"v1/TargetingCriterias/by-rule/{ruleId}") ?? new();

            var totalWeight = allCriteria.Sum(c => c.Weight ?? 0);
            var hasCriteria = allCriteria.Any();

            bool weightsMatch = Math.Abs(totalWeight - ruleWeight) < 0.0001m;

            bool isValid = hasCriteria && (weightBound ? weightsMatch : true);

            return (isValid, totalWeight, hasCriteria);
        }

        public async Task UpdateRuleValidityAsync(int ruleId, bool isValid)
        {
            var rule = await _api.GetAsync<TargetingRuleDto>($"v1/TargetingRules/{ruleId}");

            if (rule != null)
            {
                rule.IsValid = isValid;

                await _api.PutJsonAsync<TargetingRuleDto, TargetingRuleDto>(
                    $"v1/TargetingRules/{ruleId}", rule);
            }
        }
    }
}
