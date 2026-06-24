using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;
using System.Text.Json;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface ITargetingCriteriaService
    {
  
        Task<List<TargetingCriteriaDto>> GetByTargetingIdAsync(int targetingId);
        Task<TargetingCriteriaDto?> GetByIdAsync(int id);

        Task<TargetingCriteriaDto> CreateAsync(TargetingCriteriaDto dto);

        Task<TargetingCriteriaDto> UpdateAsync(int id, TargetingCriteriaDto dto);

        Task<bool> DeleteAsync(int id);
        Task<List<TargetingCriteriaDto>> GetAllAsync();

        Task<List<TargetingFieldValueDto>> GetLookupValuesAsync(int lookupId, int tenantId);


        // ================= VALIDATION =================

        // Validate expression & datatype
        //Task<bool> ValidateAsync(string expression, IndicatorDataType dataType);

        //// Generate JSON rule
        //Task<string> GenerateJsonRuleAsync(string expression);


        // ================= EXECUTION =================

        // Evaluate criteria against beneficiary data
        //Task<bool> EvaluateAsync(
        //    TargetingCriteriaDto criteria,
        //    Dictionary<string, object> parameters);

        //// Compute weighted score
        //Task<decimal> CalculateScoreAsync(
        //    TargetingCriteriaDto criteria,
        //    Dictionary<string, object> parameters);
    }
}


