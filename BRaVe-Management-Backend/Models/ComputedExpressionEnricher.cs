using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Models.es;

namespace BRaVe_Management_Backend.Models
{
    public static class ComputedExpressionEnricher
    {
        /// <summary>
        /// Enriches each household with computed field values from the given expression definitions.
        /// calc_*_hh_* → Household_Document.ComputedValues; calc_*_ind_* → Member_Document.ComputedValues (per member).
        /// </summary>
        public static void Enrich(
            List<Household_Document> households,
            Dictionary<string, ComputedExpressionDefinition> computedDefinitions)
        {
            if (households == null || computedDefinitions == null || computedDefinitions.Count == 0)
                return;

            foreach (var doc in households)
            {

                //if (doc.HouseholdId == "SOBA0100071")
                //{
                //    Dummy.DoNothing();
                //}

                foreach (var (fieldName, definition) in computedDefinitions)
                {
                    if (definition?.Expression == null) continue;

                    if (definition.Level == DTOs.IndicatorLevel.Household)
                    {
                        try
                        {
                            var value = JsonExpressionEvaluator.Evaluate(definition.Expression, doc, null);

                            if (value != null)
                            {
                                Dummy.DoNothing();
                            }
                            doc.ComputedValues ??= new Dictionary<string, object?>();
                            doc.ComputedValues[fieldName] = value;
                        }
                        catch
                        {
                            doc.ComputedValues ??= new Dictionary<string, object?>();
                            doc.ComputedValues[fieldName] = null;
                        }
                    }
                    else if (definition.Level == DTOs.IndicatorLevel.Individual)
                    {
                        foreach (var member in doc.Members ?? Enumerable.Empty<Member_Document>())
                        {
                            try
                            {
                                var value = JsonExpressionEvaluator.Evaluate(definition.Expression, doc, member);

                                //if (value != null)
                                //{
                                //    Dummy.DoNothing();
                                //}
                                member.ComputedValues ??= new Dictionary<string, object?>();
                                member.ComputedValues[fieldName] = value;
                            }
                            catch
                            {
                                member.ComputedValues ??= new Dictionary<string, object?>();
                                member.ComputedValues[fieldName] = null;
                            }
                        }
                    }
                }
            }
        }

    }
}
