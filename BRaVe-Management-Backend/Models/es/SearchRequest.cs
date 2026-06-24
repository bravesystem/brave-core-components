using System.Text.Json;
using System.Text.Json.Serialization;

namespace BRaVe_Management_Backend.Models
{
    public class SearchRequest
    {
       /* public string? Text { get; set; }
        public List<SearchFilter> Filters { get; set; } = new();
        public List<SearchSort> Sorts { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public ScoringModel? Scoring { get; init; }*/
    }

   /* public class ScoringModel
    {
        public IReadOnlyList<ScoringRule> Rules { get; init; } = [];
    }

    public class ScoringRule
    {
        public float Weight { get; init; }
        public CriteriaNode Criteria { get; init; } = default!;
    }

    public class CriteriaNode
    {
        [JsonPropertyName("operator")]
        public string? Operator { get; set; } // AND / OR / NOT

        [JsonPropertyName("rules")]
        public List<CriteriaNode>? Rules { get; set; }

        [JsonPropertyName("field")]
        public string? Field { get; set; }

        [JsonPropertyName("op")]
        public string? Operation { get; set; } // eq, gt, lt, gte, lte

        [JsonPropertyName("value")]
        public JsonElement? Value { get; set; }
    }

    public class SearchFilter
    {
        public string Field { get; set; } = default!;
        public FilterOperator Operator { get; set; }
        public object Value { get; set; } = default!;
    }

    public enum FilterOperator
    {
        Equals,
        NotEquals,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        In,
        Between
    }


    public class SearchSort
    {
        public string Field { get; set; } = default!;
        public SortDirection Direction { get; set; }
    }

    public enum SortDirection
    {
        Ascending = 0,
        Descending = 1
    }

    public class SearchResult<T>
    {
        public IReadOnlyCollection<T> Items { get; set; } = [];
        public long Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class SearchIndexOptions
    {
        public string IndexName { get; set; } = default!;
        public IReadOnlyCollection<SearchField> Fields { get; set; } = [];
    }

    public class SearchField
    {
        public string Name { get; set; } = default!;
        public SearchFieldType Type { get; set; }
        public bool IsFilterable { get; set; }
        public bool IsSortable { get; set; }
    }

    public enum SearchFieldType
    {
        Text,       // analyzed
        Keyword,    // exact
        Number,
        Date,
        Boolean
    }
    */

}
