
using BRaVe_Portal.Models;
using System.Text.RegularExpressions;
using System.Globalization;

namespace BRaVe_Portal.Services.Expressions;

public class ExpressionParser
{
    private readonly List<string> _tokens;
    private int _pos = 0;

    private ExpressionParser(List<string> tokens) => _tokens = tokens;

    public static CompositeExpressionNode Parse(string expression)
    {
        var normalized = NormalizeOperators(expression);
        var tokens = Tokenize(normalized);
        if (!tokens.Any()) throw new Exception("Empty expression");

        var parser = new ExpressionParser(tokens);
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

        NormalizeIndicatorLeafToComparison(node.Right);

        return node;
    }

    public void NormalizeIndicatorLeafToComparison(CompositeExpressionNode node)
    {
        if (node == null) return;

        var isLeaf = node.Left == null && node.Right == null && node.Fallback == null;

        if (!isLeaf)
            return;

        if (!string.Equals(node.Op, "indicator", StringComparison.OrdinalIgnoreCase))
            return;

        if (string.IsNullOrEmpty(node.Code))
            return;

        if (node.Value != null)
            return;

        // Move { Code, Op } to left node
        node.Left = new CompositeExpressionNode
        {
            Op = "indicator",
            Code = node.Code
        };

        // Right: const true
        node.Right = new CompositeExpressionNode
        {
            Op = "const",
            Code = null,
            Value = true
        };

        // This node becomes the comparison parent (eq)
        node.Op = "eq";
        node.Code = null;
        node.Value = null;
    }

    private CompositeExpressionNode ParseAnd()
    {
        var node = ParseComparison();

        NormalizeIndicatorLeafToComparison(node);

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

        if (IsBoolean(t))
        {
            return new CompositeExpressionNode
            {
                Op = "const",
                Value = bool.Parse(Advance())
            };
        }

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

    private bool IsBoolean(string t) =>
        bool.TryParse(t, out _);

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
}

