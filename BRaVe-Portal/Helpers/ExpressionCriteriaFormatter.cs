using BRaVe_Portal.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BRaVe_Portal.Services.Expressions;

/// <summary>
/// Helper to convert an expression plus weight into the
/// JSON criteria format used by simple_criteria_builder.json / complex_criteria_builder.json.
/// </summary>
public static class ExpressionCriteriaFormatter
{
    /// <summary>
    /// Parses a boolean expression and formats it as criteria JSON.
    /// </summary>
    /// <param name="expression">The boolean expression to parse.</param>
    /// <param name="weight">The score/weight for this rule.</param>
    /// <returns>JSON string representing the criteria structure.</returns>
    public static string ToCriteriaJson(string expression, double weight)
    {
        // Use the existing ExpressionParser to get the expression tree.
        var rootNode = CriteriaExpressionParser.Parse(expression);

        // Convert expression tree into criteria structure.
        var criteria = ToCriteria(rootNode);

        var root = new CriteriaRoot
        {
            Rules = new List<WeightedRule>
            {
                new WeightedRule
                {
                    Weight = weight,
                    Criteria = criteria
                }
            }
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return JsonSerializer.Serialize(root, options);
    }

    private static CriteriaNode ToCriteria(CompositeExpressionNode node)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));

        var op = node.Op?.ToLowerInvariant();
        switch (op)
        {
            case "and":
            case "or":
                return BuildLogicalCriteria(node);

            case "eq":
            case "neq":
            case "gt":
            case "gte":
            case "lt":
            case "lte":
                return BuildComparisonCriteria(node);

            case "call":
                return BuildFunctionCriteria(node);

            default:
                throw new InvalidOperationException($"Unsupported operator '{node.Op}' in criteria conversion.");
        }
    }
    private static CriteriaNode BuildFunctionCriteria(CompositeExpressionNode node)
    {
        if (string.IsNullOrEmpty(node.Code))
            throw new InvalidOperationException("Function node missing function name.");

        var functionName = node.Code.ToUpperInvariant();

        // Handle functions that take an indicator and a string/number
        var arg1 = node.Left?.Value ?? (node.Left?.Code ?? throw new InvalidOperationException("Function missing first argument"));
        var arg2 = node.Right?.Value ?? (node.Right?.Code ?? throw new InvalidOperationException("Function missing second argument"));

        string fieldName;
        object value;

        // If first argument is an indicator, use it as field
        if (node.Left?.Op?.ToLowerInvariant() == "indicator")
        {
            fieldName = ToSnakeCase(node.Left.Code!);
            value = arg2;
        }
        else
        {
            // Otherwise, fallback
            fieldName = functionName.ToLowerInvariant();
            value = new[] { arg1, arg2 };
        }

        return new CriteriaNode
        {
            FieldName = fieldName,
            FieldType = "string", // function output is usually treated as string
            Op = functionName,    // like FUZZY, LIKE, STARTSWITH, etc.
            Value = value
        };
    }


    private static CriteriaNode BuildLogicalCriteria(CompositeExpressionNode node)
    {
        var logicalOp = node.Op!.ToLowerInvariant(); // "and" / "or"

        var result = new CriteriaNode
        {
            Operator = logicalOp.ToUpperInvariant(), // "AND" / "OR"
            Rules = new List<CriteriaNode>()
        };

        void AddTerms(CompositeExpressionNode n)
        {
            if (n == null) return;

            if (string.Equals(n.Op, logicalOp, StringComparison.OrdinalIgnoreCase))
            {
                if (n.Left != null) AddTerms(n.Left);
                if (n.Right != null) AddTerms(n.Right);
            }
            else
            {
                result.Rules!.Add(ToCriteria(n));
            }
        }

        AddTerms(node);
        return result;
    }

    private static CriteriaNode BuildComparisonCriteria(CompositeExpressionNode node)
    {
        if (node.Left == null || node.Right == null)
            throw new InvalidOperationException("Comparison node must have both left and right operands.");

        CompositeExpressionNode indicatorNode;
        CompositeExpressionNode valueNode;

        if (string.Equals(node.Left.Op, "indicator", StringComparison.OrdinalIgnoreCase))
        {
            indicatorNode = node.Left;
            valueNode = node.Right;
        }
        else if (string.Equals(node.Right.Op, "indicator", StringComparison.OrdinalIgnoreCase))
        {
            indicatorNode = node.Right;
            valueNode = node.Left;
        }
        else
        {
            throw new InvalidOperationException("Comparison does not involve an indicator on either side.");
        }

        var rawFieldName = indicatorNode.Code ?? throw new InvalidOperationException("Indicator node missing code.");
        var fieldName = ToSnakeCase(rawFieldName);

        var (fieldType, value) = InferFieldTypeAndValue(valueNode.Value);

        return new CriteriaNode
        {
            FieldName = fieldName,
            FieldType = fieldType,
            Op = node.Op, // already in eq/gte/lt format expected by builder JSON
            Value = value
        };
    }

    private static (string fieldType, object? value) InferFieldTypeAndValue(object? raw)
    {
        
        if (raw is bool b) 
            return ("bool", b);

        if (raw is decimal d)
        {
            if (decimal.Truncate(d) == d)
            {
                if (d <= int.MaxValue && d >= int.MinValue)
                {
                    return ("int", (int)d);
                }

                return ("double", (double)d);
            }

            return ("double", (double)d);
        }

        if (raw is int i) return ("int", i);
        if (raw is long l) return ("int", l);
        if (raw is double dbl) return ("double", dbl);
        if (raw is float fl) return ("double", (double)fl);

        if (raw is string s)
        {
            return ("string", s);
        }

        if (raw == null) return ("string", null);

        return ("string", raw.ToString());
    }

    private static string ToSnakeCase(string code)
    {
        if (string.IsNullOrEmpty(code)) return code;

        // If the code already looks like SHOUTY_SNAKE, just lower-case it.
        if (code.All(c => char.IsUpper(c) || c == '_' || char.IsDigit(c)))
        {
            return code.ToLowerInvariant();
        }

        var sb = new StringBuilder();
        for (int i = 0; i < code.Length; i++)
        {
            var c = code[i];
            if (char.IsUpper(c))
            {
                if (i > 0 && code[i - 1] != '_')
                {
                    sb.Append('_');
                }
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    // DTOs for JSON formatting.
    private sealed class CriteriaRoot
    {
        [JsonPropertyName("rules")]
        public List<WeightedRule> Rules { get; set; } = new();
    }

    private sealed class WeightedRule
    {
        [JsonPropertyName("weight")]
        public double Weight { get; set; }

        [JsonPropertyName("criteria")]
        public CriteriaNode Criteria { get; set; } = null!;
    }

    private sealed class CriteriaNode
    {
        // Logical group: { "operator": "...", "rules": [ ... ] }
        [JsonPropertyName("operator")]
        public string? Operator { get; set; }

        [JsonPropertyName("rules")]
        public List<CriteriaNode>? Rules { get; set; }

        // Leaf rule: { "field_name": "...", "field_type": "...", "op": "...", "value": ... }
        [JsonPropertyName("field_name")]
        public string? FieldName { get; set; }

        [JsonPropertyName("field_type")]
        public string? FieldType { get; set; }

        [JsonPropertyName("op")]
        public string? Op { get; set; }

        [JsonPropertyName("value")]
        public object? Value { get; set; }
    }
}

