
using BRaVe_Portal.Models;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace BRaVe_Portal.Services.Expressions;

public class CriteriaExpressionParser
{
    private readonly List<string> _tokens;
    private int _pos = 0;

    private CriteriaExpressionParser(List<string> tokens) => _tokens = tokens;

    public static CompositeExpressionNode Parse(string expression)
    {
        var normalized = NormalizeOperators(expression);
        var tokens = Tokenize(normalized);
        if (!tokens.Any()) throw new Exception("Empty expression");

        var parser = new CriteriaExpressionParser(tokens);
        var node = parser.ParseExpression();

        if (!parser.End()) throw new Exception("Unexpected token: " + parser.Peek());
        return node;
    }

    private static string NormalizeOperators(string s) => s
        .Replace("&gt;=", ">=")
        .Replace("&lt;=", "<=")
        .Replace("&gt;", ">")
        .Replace("&lt;", "<");

    private CompositeExpressionNode ParseExpression()
    {
        if (Match("IF")) return ParseIf();

        return ParseOr();
    }
    private CompositeExpressionNode ParseOr()
    {
        var node = ParseAnd();

        while (Match("OR"))
        {
            var right = ParseAnd();

            node = new CompositeExpressionNode
            {
                Op = "or",
                Left = node,
                Right = right
            };
        }

        return node;
    }
    private CompositeExpressionNode ParseAnd()
    {
        var node = ParseComparison();

        while (Match("AND"))
        {
            var right = ParseComparison();

            node = new CompositeExpressionNode
            {
                Op = "and",
                Left = node,
                Right = right
            };
        }

        return node;
    }


    private CompositeExpressionNode ParseIf()
    {
        Consume("(");
        var condition = ParseComparison(); // or ParseExpression for nested logic
        Consume(",");
        var whenTrue = ParseExpression();
        Consume(",");
        var whenFalse = ParseExpression();
        Consume(")");

        return new CompositeExpressionNode
        {
            Op = "if",
            Left = condition,
            Right = new CompositeExpressionNode
            {
                Op = "then",
                Left = whenTrue,
                Right = whenFalse
            }
        };
    }

    private CompositeExpressionNode ParseComparison()
    {
        var node = ParseMath();

        if (Match(">", "<", ">=", "<=", "==", "!="))
        {
            var op = Previous();
            var right = ParseMath();
            return new CompositeExpressionNode
            {
                Op = MapCompare(op),
                Left = node,
                Right = right
            };
        }
        return node;
    }

    private CompositeExpressionNode ParseMath()
    {
        var node = ParseTerm();
        while (Match("+", "-"))
        {
            var op = Previous();
            var right = ParseTerm();
            node = new CompositeExpressionNode { Op = MapOperator(op), Left = node, Right = right };
        }
        return node;
    }

    private CompositeExpressionNode ParseTerm()
    {
        var node = ParseUnary();
        while (Match("*", "/", "%"))
        {
            var op = Previous();
            var right = ParseUnary();
            node = new CompositeExpressionNode { Op = MapOperator(op), Left = node, Right = right };
        }
        return node;
    }

    // NEW
    private CompositeExpressionNode ParseUnary()
    {
        if (Match("+")) return ParseUnary();
        if (Match("-"))
        {
            var right = ParseUnary();
            return new CompositeExpressionNode { Op = "negate", Left = right };
        }
        return ParseFactor();
    }

    private CompositeExpressionNode ParseFactor()
    {
        if (Match("("))
        {
            var expr = ParseExpression();
            Consume(")");
            return expr;
        }

        if (End()) throw new Exception("Unexpected end of expression");

        var t = Peek();

        if (IsNumber(t))
        {
            return new CompositeExpressionNode
            {
                Op = "const",
                Value = decimal.Parse(Advance(), NumberStyles.Number, CultureInfo.InvariantCulture)
            };
        }

        if (IsIndicator(t))
        {
            return new CompositeExpressionNode
            {
                Op = "indicator",
                Code = Advance().Trim('[', ']')
            };
        }


        if (bool.TryParse(t, out bool value))
        {
            Advance();
            return new CompositeExpressionNode
            {
                Op = "const",
                Value = value
            };
        }


        // NEW: string literal
        if (t.StartsWith("'") && t.EndsWith("'"))
        {
            return new CompositeExpressionNode
            {
                Op = "const",
                Value = Advance().Trim('\'') // remove quotes
            };
        }

        if (IsIdentifier(t))
            return ParseCallOrIdentifier();

        throw new Exception($"Unexpected token: {t}");
    }


    // Function call support (optional)
    private CompositeExpressionNode ParseCallOrIdentifier()
    {
        var name = Advance();
        if (Match("("))
        {
            var args = new List<CompositeExpressionNode>();
            if (!Match(")"))
            {
                do { args.Add(ParseExpression()); } while (Match(","));
                Consume(")");
            }
            return new CompositeExpressionNode { Op = "call", Code = name, Left = args.FirstOrDefault(), Right = args.Skip(1).FirstOrDefault() };
        }
        return new CompositeExpressionNode { Op = "identifier", Code = name };
    }

    private string MapCompare(string op) => op switch
    {
        ">" => "gt",
        "<" => "lt",
        ">=" => "gte",
        "<=" => "lte",
        "==" => "eq",
        "!=" => "neq",
        _ => throw new Exception("Invalid comparison " + op)
    };

    private static List<string> Tokenize(string expr)
    {
        var regex = new Regex(
            @"IF
        |>=|<=|==|!=|>|<
        |\[[A-Za-z0-9_]+\]                  # indicator
        |'[^']*'                           # string literal
        |\d+(\.\d+)?                        # number
        |[A-Za-z_][A-Za-z0-9_]*             # identifier
        |[(),+\-*/%]                        # operators
        ",
            RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        return regex.Matches(expr)
                    .Select(m => m.Value)
                    .ToList();
    }

    private bool Match(params string[] tokens)
    {
        if (End()) return false;
        var peek = Peek();
        if (tokens.Any(t => string.Equals(peek, t, StringComparison.OrdinalIgnoreCase)))
        {
            Advance();
            return true;
        }
        return false;
    }

    private void Consume(string token)
    {
        if (!Match(token))
            throw new Exception($"Expected '{token}' at token #{_pos}, found '{(End() ? "<eof>" : Peek())}'");
    }

    private string Advance() => _tokens[_pos++];
    private string Peek() => _tokens[_pos];
    private string Previous() => _tokens[_pos - 1];
    private bool End() => _pos >= _tokens.Count;

    private bool IsNumber(string t) =>
        decimal.TryParse(t, NumberStyles.Number, CultureInfo.InvariantCulture, out _);

    private bool IsIndicator(string t) =>
        t.StartsWith("[") && t.EndsWith("]");

    private bool IsIdentifier(string t) =>
        Regex.IsMatch(t, @"^[A-Za-z_][A-Za-z0-9_]*$");

    private string MapOperator(string op) => op switch
    {
        "+" => "add",
        "-" => "subtract",
        "*" => "multiply",
        "/" => "divide",
        "%" => "mod",
        _ => throw new Exception("Unknown operator " + op)
    };

    /// <summary>
    /// Parses a boolean expression and formats it as a criteria JSON
    /// similar to simple_criteria_builder.json / complex_criteria_builder.json.
    /// </summary>
    /// <param name="expression">The boolean expression to parse.</param>
    /// <param name="weight">The score/weight for this rule.</param>
    /// <returns>JSON string representing the criteria structure.</returns>
    public static string Parse(string expression, double weight)
    {
        // Reuse the existing parser to build the expression tree.
        var rootNode = Parse(expression);

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

            default:
                throw new InvalidOperationException($"Unsupported operator '{node.Op}' in criteria conversion.");
        }
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
        if (raw is string s)
        {
            return ("string", s);
        }

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

