using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Interfaces;
using Serilog;
using System.Security.Cryptography;
using System.Text;

namespace BRaVe_Management_Backend.Helpers
{
    public class IndicatorComputationService : IIndicatorComputationService
    {
        private readonly IIndicatorRepositoryService _repo;
        //private readonly IRawIndicatorResolverService _resolver;
        private readonly ICompositeEvaluatorService _evaluator;

        public IndicatorComputationService(IIndicatorRepositoryService repo/*, IRawIndicatorResolverService resolver*/, ICompositeEvaluatorService evaluator)
        {
            _repo = repo;
            //_resolver = resolver;
            //_evaluator = evaluator;
        }

        public async Task<List<ComputedIndicatorValueDto>> ComputeAsync(int tenantId, TargetingRunDto run, List<Guid> entityIds)
        {
            var indicators = await _repo.GetActiveIndicatorsAsync(tenantId);
            var composites = await _repo.GetCompositeIndicatorsAsync(tenantId);
            var results = new List<ComputedIndicatorValueDto>();

            foreach (var entityId in entityIds)
            {
                var values = new Dictionary<string, decimal?>();

                // Resolve raw indicators
                /*foreach (var raw in indicators.Where(i => i.IndicatorType == IndicatorType.BuiltIn))
                    values[raw.Code] = await _resolver.ResolveAsync(raw.Code, entityId);*/

                // Evaluate composites
                foreach (var composite in composites.OrderBy(c => c.CalculationOrder))
                {
                    var indicator = indicators.First(i => i.IndicatorId == composite.IndicatorId);
                    values[indicator.Code] = _evaluator.Evaluate(composite.Expression, values);
                }

                // Populate results
                foreach (var indicator in indicators)
                    results.Add(new ComputedIndicatorValueDto
                    {
                        RunId = run.RunId,
                        IndicatorId = indicator.IndicatorId,
                        EntityId = entityId,
                        Value = values.GetValueOrDefault(indicator.Code)
                    });
            }

            return results;
        }

        
    }
}
