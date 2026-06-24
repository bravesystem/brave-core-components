using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BRaVe_Portal.Services
{
    /// <summary>
    /// Parser for computed field expressions.
    /// Converts expression strings to JSON format matching ComputedExpressionDefinition.
    /// </summary>
    /// <summary>
    /// Parser for computed field expressions.
    /// Converts expression strings to JSON format matching ComputedExpressionDefinition.
    /// </summary>
    public static class ComputedExpressionParser
    {
        /// <summary>
        /// Parses an expression string and converts it to JSON format.
        /// </summary>
        /// <param name="indicators">List of indicators to resolve field types</param>
        /// <param name="expression">Expression string to parse</param>
        /// <param name="outputDataType">Expected output data type</param>
        /// <returns>JSON string representing the expression</returns>
        public static string ParseToJson(List<IndicatorDto> indicators, string expression, IndicatorDataType outputDataType)
        {
            var indicatorMap = indicators.ToDictionary(i => i.Code, i => i.DataType);
            var parser = new ExpressionParser(expression, indicatorMap);
            var expr = parser.ParseExpression();
            var referencedFields = parser.GetReferencedFields();

            var definition = new ComputedExpressionDefinition
            {
                Inputs = referencedFields.Select(field => new ComputedInput
                {
                    Field = field,
                    Type = MapDataTypeToJsonType(indicatorMap.GetValueOrDefault(field, IndicatorDataType.Number)),
                    Required = true
                }).ToList(),
                Expression = expr,
                ReturnType = MapDataTypeToJsonType(outputDataType)
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = null // Explicitly set to null to respect JsonPropertyName attributes
            };

            return JsonSerializer.Serialize(definition, options);
        }

        private static string MapDataTypeToJsonType(IndicatorDataType dataType)
        {
            return dataType switch
            {
                IndicatorDataType.Number => "int",
                IndicatorDataType.Numeric => "double",
                IndicatorDataType.String => "string",
                IndicatorDataType.Boolean => "bool",
                IndicatorDataType.Date => "datetime",
                _ => "int"
            };
        }

        /// <summary>
        /// Internal parser class for parsing expressions.
        /// </summary>
        private class ExpressionParser
        {
            private readonly string _expression;
            private readonly Dictionary<string, IndicatorDataType> _indicatorMap;
            private int _position;
            private readonly HashSet<string> _referencedFields = new();

            public ExpressionParser(string expression, Dictionary<string, IndicatorDataType> indicatorMap)
            {
                _expression = expression?.Trim() ?? string.Empty;
                _indicatorMap = indicatorMap;
                _position = 0;
            }

            public HashSet<string> GetReferencedFields() => _referencedFields;

            public ComputedExpressionBase ParseExpression()
            {
                _position = 0;
                SkipWhitespace();
                var expr = ParseTernary();
                if (_position < _expression.Length)
                {
                    throw new ParseException($"Unexpected characters at position {_position}: {_expression.Substring(_position)}");
                }

                return expr;
            }

            private ComputedExpressionBase ParseTernary()
            {
                SkipWhitespace();

                // Check for standalone comparison expression
                var comparison = TryParseStandaloneComparison();
                if (comparison != null)
                {
                    return comparison;
                }

                return ParseArithmetic();
            }

            private ComparisonExpression? TryParseStandaloneComparison()
            {
                var savedPosition = _position;
                SkipWhitespace();

                // Try to parse a field reference
                if (Peek() != '[')
                {
                    _position = savedPosition;
                    return null;
                }

                var fieldRef = ParseFieldReference();
                if (!(fieldRef is FieldReferenceExpression fieldExpr))
                {
                    _position = savedPosition;
                    return null;
                }

                SkipWhitespace();

                // Check for comparison operator
                string? op = null;
                if (Match("=="))
                {
                    op = "eq";
                }
                else if (Match("!="))
                {
                    op = "ne";
                }
                else if (Match("<="))
                {
                    op = "lte";
                }
                else if (Match(">="))
                {
                    op = "gte";
                }
                else if (Match("fuzzy"))
                {
                    op = "fuzzy";
                }
                else if (Peek() == '<')
                {
                    ReadChar();
                    op = "lt";
                }
                else if (Peek() == '>')
                {
                    ReadChar();
                    op = "gt";
                }
                else
                {
                    _position = savedPosition;
                    return null;
                }

                SkipWhitespace();
                var value = ParseLiteralValue();
                SkipWhitespace(); // Consume any trailing whitespace

                return new ComparisonExpression
                {
                    Field = fieldExpr.Field,
                    Op = op,
                    Value = value
                };
            }

            private ComputedExpressionBase ParseArithmeticFactor()
            {
                SkipWhitespace();

                if (Peek() == '(')
                {
                    ReadChar(); // consume '('
                    var expr = ParseTernary();
                    Expect(")");
                    return expr;
                }

                if (Match("if("))
                {
                    var condition = ParseComparison();
                    Expect(",");
                    SkipWhitespace();
                    var thenValue = ParseTernaryValue();
                    Expect(",");
                    SkipWhitespace();
                    var elseValue = ParseTernaryValue();
                    Expect(")");

                    return new TernaryExpression
                    {
                        Condition = condition,
                        ThenValue = ConvertToJsonElement(thenValue),
                        ElseValue = ConvertToJsonElement(elseValue)
                    };
                }

                if (Peek() == '[')
                {
                    return ParseFieldReference();
                }

                // Check for function call
                if (char.IsLetter(Peek()))
                {
                    var funcName = ReadIdentifier();
                    SkipWhitespace(); // Allow space between function name and '(' e.g. "NOT ("
                    if (Peek() == '(')
                    {
                        return ParseFunctionCall(funcName);
                    }
                    throw new ParseException($"Unexpected identifier '{funcName}' at position {_position}");
                }

                // Try to parse as literal
                return ParseLiteral();
            }

            private ComputedExpressionBase ParseTernaryValue()
            {
                // Parse as a general expression value (supports nested if and arithmetic)
                return ParseArithmetic();
            }

            /*private ComputedExpressionBase ParseTernaryValue()
            {
                SkipWhitespace();

                // Check if it's a nested ternary
                if (Match("if("))
                {
                    var condition = ParseComparison();
                    Expect(",");
                    SkipWhitespace();
                    var thenValue = ParseTernaryValue();
                    Expect(",");
                    SkipWhitespace();
                    var elseValue = ParseTernaryValue();
                    Expect(")");

                    return new TernaryExpression
                    {
                        Condition = condition,
                        ThenValue = ConvertToJsonElement(thenValue),
                        ElseValue = ConvertToJsonElement(elseValue)
                    };
                }

                // Otherwise parse as arithmetic/literal
                return ParseArithmetic();
            }*/

            private ComputedExpressionBase ParseArithmetic()
            {
                var left = ParseArithmeticTerm();
                SkipWhitespace();

                while (_position < _expression.Length)
                {
                    if (Peek() == '+' || Peek() == '-')
                    {
                        var op = ReadChar();
                        SkipWhitespace();
                        var right = ParseArithmeticTerm();
                        left = new ArithmeticExpression
                        {
                            Op = op == '+' ? "add" : "subtract",
                            Left = ConvertToOperand(left),
                            Right = ConvertToOperand(right)
                        };
                    }
                    else if (Peek() == ')' || Peek() == ',')
                    {
                        break;
                    }
                    else
                    {
                        throw new ParseException($"Unexpected character '{Peek()}' at position {_position}");
                    }
                }

                return left;
            }

            private ComputedExpressionBase ParseArithmeticTerm()
            {
                var left = ParseArithmeticFactor();
                SkipWhitespace();

                while (_position < _expression.Length && (Peek() == '*' || Peek() == '/'))
                {
                    var op = ReadChar();
                    SkipWhitespace();
                    var right = ParseArithmeticFactor();
                    left = new ArithmeticExpression
                    {
                        Op = op == '*' ? "multiply" : "divide",
                        Left = ConvertToOperand(left),
                        Right = ConvertToOperand(right)
                    };
                }

                return left;
            }

            /*private ComputedExpressionBase ParseArithmeticFactor()
            {
                SkipWhitespace();

                if (Peek() == '(')
                {
                    ReadChar(); // consume '('
                    var expr = ParseTernary();
                    Expect(")");
                    return expr;
                }

                if (Peek() == '[')
                {
                    return ParseFieldReference();
                }

                // Check for function call
                if (char.IsLetter(Peek()))
                {
                    var funcName = ReadIdentifier();
                    SkipWhitespace(); // Allow space between function name and '(' e.g. "NOT ("
                    if (Peek() == '(')
                    {
                        return ParseFunctionCall(funcName);
                    }
                    throw new ParseException($"Unexpected identifier '{funcName}' at position {_position}");
                }

                // Try to parse as literal
                return ParseLiteral();
            }*/

            private ComputedExpressionBase ParseFieldReference()
            {
                Expect("[");
                var fieldName = ReadUntil(']');
                Expect("]");
                _referencedFields.Add(fieldName);
                return new FieldReferenceExpression { Field = fieldName };
            }

            private ComputedExpressionBase ParseFunctionCall(string functionName)
            {
                Expect("(");
                var args = new List<FunctionArgument>();

                SkipWhitespace();
                if (Peek() != ')')
                {
                    while (true)
                    {
                        var arg = ParseFunctionArgument();
                        args.Add(arg);
                        SkipWhitespace();
                        if (Peek() == ')')
                            break;
                        Expect(",");
                        SkipWhitespace();
                    }
                }
                Expect(")");

                // Special handling for comparison functions like fuzzy
                if (functionName == "fuzzy" && args.Count == 2)
                {
                    var fieldArg = args[0];
                    var valueArg = args[1];

                    if (fieldArg.FieldRef == null)
                        throw new ParseException("fuzzy() first argument must be a field reference");

                    return new ComparisonExpression
                    {
                        Field = fieldArg.FieldRef.Field,
                        Op = "fuzzy",
                        Value = valueArg.Literal ?? JsonSerializer.SerializeToElement("")
                    };
                }

                return new FunctionExpression
                {
                    Name = functionName,
                    Args = args
                };
            }

            private FunctionArgument ParseFunctionArgument()
            {
                SkipWhitespace();

                if (Peek() == '[')
                {
                    // Try comparison first (e.g. NOT([age_years] > 17)), else plain field ref (e.g. NOT([has_biometric]))
                    var comparison = TryParseStandaloneComparison();
                    if (comparison != null)
                    {
                        return new FunctionArgument { Expression = comparison };
                    }
                    var fieldRef = ParseFieldReference();
                    return new FunctionArgument { FieldRef = new FieldReference { Field = ((FieldReferenceExpression)fieldRef).Field } };
                }

                if (Peek() == '(' || char.IsLetter(Peek()))
                {
                    // Could be nested expression or function call
                    var expr = ParseArithmeticFactor();
                    return new FunctionArgument { Expression = expr };
                }

                // Parse literal
                var literal = ParseLiteralValue();
                return new FunctionArgument { Literal = literal };
            }

            private ComparisonCondition ParseComparison()
            {
                var left = ParseComparisonLeft();
                SkipWhitespace();

                string op;
                if (Match("=="))
                {
                    op = "eq";
                }
                else if (Match("!="))
                {
                    op = "ne";
                }
                else if (Match("<="))
                {
                    op = "lte";
                }
                else if (Match(">="))
                {
                    op = "gte";
                }
                else if (Peek() == '<')
                {
                    ReadChar();
                    op = "lt";
                }
                else if (Peek() == '>')
                {
                    ReadChar();
                    op = "gt";
                }
                else
                {
                    throw new ParseException($"Expected comparison operator at position {_position}");
                }

                SkipWhitespace();
                var right = ParseComparisonRight();

                if (left is FieldReferenceExpression fieldRef)
                {
                    return new ComparisonCondition
                    {
                        Field = fieldRef.Field,
                        Op = op,
                        Value = right
                    };
                }

                throw new ParseException("Comparison left side must be a field reference");
            }

            private ComputedExpressionBase ParseComparisonLeft()
            {
                SkipWhitespace();
                if (Peek() == '[')
                {
                    return ParseFieldReference();
                }
                throw new ParseException("Comparison left side must be a field reference");
            }

            private JsonElement ParseComparisonRight()
            {
                SkipWhitespace();
                return ParseLiteralValue();
            }

            private ComputedExpressionBase ParseLiteral()
            {
                var value = ParseLiteralValue();
                return new LiteralExpression { Value = value };
            }

            private JsonElement ParseLiteralValue()
            {
                SkipWhitespace();

                // Try boolean first (before checking for quotes, as 'true' could start with quote)
                if (Match("true"))
                {
                    SkipWhitespace();
                    return JsonSerializer.SerializeToElement(true);
                }
                if (Match("false"))
                {
                    SkipWhitespace();
                    return JsonSerializer.SerializeToElement(false);
                }

                if (Peek() == '\'' || Peek() == '"')
                {
                    var quote = ReadChar();
                    var str = ReadUntil(quote);
                    ReadChar(); // consume closing quote
                    return JsonSerializer.SerializeToElement(str);
                }

                if (char.IsDigit(Peek()) || Peek() == '-' || Peek() == '.')
                {
                    var numStr = ReadNumber();
                    if (numStr.Contains('.'))
                    {
                        if (double.TryParse(numStr, out var dbl))
                            return JsonSerializer.SerializeToElement(dbl);
                    }
                    else
                    {
                        if (int.TryParse(numStr, out var num))
                            return JsonSerializer.SerializeToElement(num);
                    }
                    throw new ParseException($"Invalid number: {numStr}");
                }

                throw new ParseException($"Unexpected character '{Peek()}' at position {_position}");
            }

            private char Peek() => _position < _expression.Length ? _expression[_position] : '\0';
            private char ReadChar() => _expression[_position++];

            private void SkipWhitespace()
            {
                while (_position < _expression.Length && char.IsWhiteSpace(_expression[_position]))
                    _position++;
            }

            private bool Match(string str)
            {
                if (_position + str.Length > _expression.Length)
                    return false;
                if (_expression.Substring(_position, str.Length) == str)
                {
                    _position += str.Length;
                    return true;
                }
                return false;
            }

            private void Expect(string str)
            {
                if (!Match(str))
                    throw new ParseException($"Expected '{str}' at position {_position}");
            }

            private string ReadUntil(char endChar)
            {
                var start = _position;
                while (_position < _expression.Length && _expression[_position] != endChar)
                    _position++;
                if (_position >= _expression.Length)
                    throw new ParseException($"Expected '{endChar}' but reached end of expression");
                return _expression.Substring(start, _position - start);
            }

            private string ReadIdentifier()
            {
                var start = _position;
                while (_position < _expression.Length && (char.IsLetterOrDigit(_expression[_position]) || _expression[_position] == '_'))
                    _position++;
                return _expression.Substring(start, _position - start);
            }

            private string ReadNumber()
            {
                var start = _position;
                if (Peek() == '-')
                    ReadChar();
                while (_position < _expression.Length && (char.IsDigit(_expression[_position]) || _expression[_position] == '.'))
                    ReadChar();
                return _expression.Substring(start, _position - start);
            }

            private ArithmeticOperand ConvertToOperand(ComputedExpressionBase expr)
            {
                if (expr is FieldReferenceExpression fieldRef)
                {
                    return new ArithmeticOperand { FieldRef = new FieldReference { Field = fieldRef.Field } };
                }
                if (expr is LiteralExpression literal)
                {
                    if (literal.Value.ValueKind == JsonValueKind.Number)
                    {
                        return new ArithmeticOperand { Literal = literal.Value.GetDouble() };
                    }
                }
                return new ArithmeticOperand { Expression = expr };
            }

            private JsonElement ConvertToJsonElement(ComputedExpressionBase expr)
            {
                if (expr is LiteralExpression literal)
                {
                    return literal.Value;
                }
                if (expr is FieldReferenceExpression fieldRef)
                {
                    // Field references in then/else values should be converted to their actual values
                    // For now, we'll serialize as string representation
                    throw new ParseException("Field references not allowed as literal values in ternary expressions");
                }
                // For nested expressions, serialize to JsonElement
                var json = JsonSerializer.Serialize(expr, new JsonSerializerOptions());
                return JsonDocument.Parse(json).RootElement.Clone();
            }
        }

        // Helper expression classes for parsing
        private class FieldReferenceExpression : ComputedExpressionBase
        {
            public override string Type => throw new NotImplementedException();
            public string Field { get; set; } = string.Empty;
        }

        private class LiteralExpression : ComputedExpressionBase
        {
            public override string Type => throw new NotImplementedException();
            public JsonElement Value { get; set; }
        }

        private class ParseException : Exception
        {
            public ParseException(string message) : base(message) { }
        }
    }


}
