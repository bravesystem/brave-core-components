using BRaVe_Management_Backend.Models;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace BRaVe_Management_Backend.Helpers.es
{
    public static class LuceneQueryHelper
    {
    /*    public static Query BuildLuceneQueryFromCriteria(CriteriaNode node)
        {
            if (node.Rules == null || !node.Rules.Any())
            {
                return BuildLeafQuery(node);
            }

            var booleanQuery = new BooleanQuery();

            var occur = node.Operator switch
            {
                "AND" => Occur.MUST,
                "OR" => Occur.SHOULD,
                "NOT" => Occur.MUST_NOT,
                _ => Occur.MUST
            };

            foreach (var rule in node.Rules)
            {
                var childQuery = BuildLuceneQueryFromCriteria(rule);
                booleanQuery.Add(childQuery, occur);
            }

            // OR groups need at least one match
            if (node.Operator == "OR")
                booleanQuery.MinimumNumberShouldMatch = 1;

            return booleanQuery;
        }

        private static Query BuildLeafQuery(CriteriaNode node)
        {
            return node.Operation switch
            {
                "eq" => new TermQuery(new Term(
                    node.Field!,
                    node.Value!.Value.GetString()!.ToLowerInvariant())),

                "gt" => NumericRangeQuery.NewInt32Range(
                    node.Field!,
                    int.Parse(node.Value!.Value.GetString()!),
                    null,
                    false,
                    false),

                "lt" => NumericRangeQuery.NewInt32Range(
                    node.Field!,
                    null,
                    int.Parse(node.Value!.Value.GetString()!),
                    false,
                    false),

                _ => throw new NotSupportedException($"Unsupported op {node.Operation}")
            };
        }*/


    }
}
