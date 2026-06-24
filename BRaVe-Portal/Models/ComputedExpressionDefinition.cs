using BRaVe_Portal.Models.DTOs;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BRaVe_Portal.Models
{
    public class ComputedExpressionDefinition
    {
        [JsonPropertyName("inputs")]
        public List<ComputedInput> Inputs { get; set; } = new();

        [JsonPropertyName("expression")]
        [JsonConverter(typeof(ExpressionConverter))]
        public ComputedExpressionBase Expression { get; set; } = null!;

        [JsonPropertyName("return_type")]
        public string ReturnType { get; set; } = "int"; // int | long | double | string | bool | datetime
    }

    /// <summary>
    /// Input field declaration for a computed expression.
    /// </summary>
    public class ComputedInput
    {
        [JsonPropertyName("field")]
        public string Field { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = "int"; // int | double | string | bool | datetime

        [JsonPropertyName("required")]
        public bool Required { get; set; } = true;
    }

    /// <summary>
    /// Comparison condition used in ternary and switch expressions.
    /// </summary>
    public class ComparisonCondition
    {
        [JsonPropertyName("field")]
        public string Field { get; set; } = string.Empty;

        [JsonPropertyName("op")]
        public string Op { get; set; } = string.Empty; // eq, ne, lt, lte, gt, gte, contains, like, fuzzy

        [JsonPropertyName("value")]
        public JsonElement Value { get; set; } // supports int, double, string, bool
    }

    /// <summary>
    /// Base type for expression nodes.
    /// </summary>
    [JsonConverter(typeof(ExpressionConverter))]
    public abstract class ComputedExpressionBase
    {
        [JsonPropertyName("type")]
        public abstract string Type { get; }
    }

    /// <summary>
    /// Ternary: condition ? then_value : else_value
    /// </summary>
    public class TernaryExpression : ComputedExpressionBase
    {
        public override string Type => "ternary";

        [JsonPropertyName("condition")]
        public ComparisonCondition Condition { get; set; } = null!;

        [JsonPropertyName("then_value")]
        public JsonElement ThenValue { get; set; }

        [JsonPropertyName("else_value")]
        public JsonElement ElseValue { get; set; }
    }

    /// <summary>
    /// Arithmetic: left op right (add, subtract, multiply, divide)
    /// Left/right can be literal, field reference, or nested expression.
    /// </summary>
    public class ArithmeticExpression : ComputedExpressionBase
    {
        public override string Type => "arithmetic";

        [JsonPropertyName("op")]
        public string Op { get; set; } = string.Empty; // add, subtract, multiply, divide

        [JsonPropertyName("left")]
        [JsonConverter(typeof(ArithmeticOperandConverter))]
        public ArithmeticOperand Left { get; set; } = null!;

        [JsonPropertyName("right")]
        [JsonConverter(typeof(ArithmeticOperandConverter))]
        public ArithmeticOperand Right { get; set; } = null!;
    }

    /// <summary>
    /// Switch: multiple cases with default.
    /// </summary>
    public class SwitchExpression : ComputedExpressionBase
    {
        public override string Type => "switch";

        [JsonPropertyName("cases")]
        public List<SwitchCase> Cases { get; set; } = new();

        [JsonPropertyName("default")]
        public JsonElement Default { get; set; }
    }

    /// <summary>
    /// Comparison expression: standalone comparison that returns a bool.
    /// Used for expressions like fuzzy([field_a], 'sometext').
    /// </summary>
    public class ComparisonExpression : ComputedExpressionBase
    {
        public override string Type => "comparison";

        [JsonPropertyName("field")]
        public string Field { get; set; } = string.Empty;

        [JsonPropertyName("op")]
        public string Op { get; set; } = string.Empty; // eq, ne, lt, lte, gt, gte, contains, like, fuzzy

        [JsonPropertyName("value")]
        public JsonElement Value { get; set; } // supports int, double, string, bool
    }

    /// <summary>
    /// Function expression: custom function call like func([field_a], [Field_b]).
    /// The system will handle the function name and arguments.
    /// </summary>
    [JsonConverter(typeof(FunctionExpressionConverter))]
    public class FunctionExpression : ComputedExpressionBase
    {
        public override string Type => "function";

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("args")]
        public List<FunctionArgument> Args { get; set; } = new();
    }

    /// <summary>
    /// Argument in a function call: can be a field reference, literal value, or nested expression.
    /// </summary>
    public class FunctionArgument
    {
        public FieldReference? FieldRef { get; set; }
        public JsonElement? Literal { get; set; }
        public ComputedExpressionBase? Expression { get; set; }
    }

    /// <summary>
    /// Single case in a switch expression.
    /// </summary>
    public class SwitchCase
    {
        [JsonPropertyName("condition")]
        public ComparisonCondition Condition { get; set; } = null!;

        [JsonPropertyName("value")]
        public JsonElement Value { get; set; }
    }

    /// <summary>
    /// Operand in arithmetic: literal number, field reference, or nested expression.
    /// </summary>
    public class ArithmeticOperand
    {
        public double? Literal { get; set; }
        public FieldReference? FieldRef { get; set; }
        public ComputedExpressionBase? Expression { get; set; }
    }

    /// <summary>
    /// Field reference: { "field": "field_name" }
    /// </summary>
    public class FieldReference
    {
        [JsonPropertyName("field")]
        public string Field { get; set; } = string.Empty;
    }

    /// <summary>
    /// Converts polymorphic expression JSON to the correct expression type.
    /// </summary>
    public class ExpressionConverter : JsonConverter<ComputedExpressionBase>
    {
        public override ComputedExpressionBase? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;
            if (!root.TryGetProperty("type", out var typeProp))
                throw new JsonException("Expression missing 'type' property.");

            var type = typeProp.GetString();
            return type switch
            {
                "ternary" => JsonSerializer.Deserialize<TernaryExpression>(root.GetRawText(), options),
                "arithmetic" => JsonSerializer.Deserialize<ArithmeticExpression>(root.GetRawText(), options),
                "switch" => JsonSerializer.Deserialize<SwitchExpression>(root.GetRawText(), options),
                "comparison" => JsonSerializer.Deserialize<ComparisonExpression>(root.GetRawText(), options),
                "function" => JsonSerializer.Deserialize<FunctionExpression>(root.GetRawText(), options),
                _ => throw new JsonException($"Unknown expression type: {type}")
            };
        }

        public override void Write(Utf8JsonWriter writer, ComputedExpressionBase value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }

    /// <summary>
    /// Converts arithmetic operands: literal number, field ref, or nested expression.
    /// </summary>
    public class ArithmeticOperandConverter : JsonConverter<ArithmeticOperand>
    {
        public override ArithmeticOperand Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var result = new ArithmeticOperand();

            if (reader.TokenType == JsonTokenType.Number)
            {
                result.Literal = reader.GetDouble();
                return result;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Operand must be number or object.");

            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            if (root.TryGetProperty("type", out _))
            {
                result.Expression = JsonSerializer.Deserialize<ComputedExpressionBase>(root.GetRawText(), options);
            }
            else if (root.TryGetProperty("field", out var fieldProp))
            {
                result.FieldRef = new FieldReference { Field = fieldProp.GetString() ?? string.Empty };
            }
            else
            {
                throw new JsonException("Operand object must have 'type' (expression) or 'field' (field reference).");
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, ArithmeticOperand value, JsonSerializerOptions options)
        {
            if (value.Literal.HasValue)
            {
                writer.WriteNumberValue(value.Literal.Value);
            }
            else if (value.FieldRef != null)
            {
                JsonSerializer.Serialize(writer, value.FieldRef, options);
            }
            else if (value.Expression != null)
            {
                JsonSerializer.Serialize(writer, value.Expression, options);
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }

    /// <summary>
    /// Converts function expressions, handling the args array properly.
    /// </summary>
    public class FunctionExpressionConverter : JsonConverter<FunctionExpression>
    {
        public override FunctionExpression Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            var funcExpr = new FunctionExpression();

            if (root.TryGetProperty("name", out var nameProp))
            {
                funcExpr.Name = nameProp.GetString() ?? string.Empty;
            }

            if (root.TryGetProperty("args", out var argsProp) && argsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var argElement in argsProp.EnumerateArray())
                {
                    var arg = new FunctionArgument();

                    // Check if it's a field reference
                    if (argElement.TryGetProperty("field", out var fieldProp))
                    {
                        arg.FieldRef = new FieldReference { Field = fieldProp.GetString() ?? string.Empty };
                    }
                    // Check if it's an expression (has "type" property)
                    else if (argElement.TryGetProperty("type", out _))
                    {
                        arg.Expression = JsonSerializer.Deserialize<ComputedExpressionBase>(argElement.GetRawText(), options);
                    }
                    // Otherwise, treat as literal
                    else
                    {
                        arg.Literal = argElement.Clone();
                    }

                    funcExpr.Args.Add(arg);
                }
            }

            return funcExpr;
        }

        public override void Write(Utf8JsonWriter writer, FunctionExpression value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("type", value.Type);
            writer.WriteString("name", value.Name);
            writer.WritePropertyName("args");
            writer.WriteStartArray();

            foreach (var arg in value.Args)
            {
                if (arg.FieldRef != null)
                {
                    JsonSerializer.Serialize(writer, arg.FieldRef, options);
                }
                else if (arg.Expression != null)
                {
                    JsonSerializer.Serialize(writer, arg.Expression, options);
                }
                else if (arg.Literal.HasValue)
                {
                    arg.Literal.Value.WriteTo(writer);
                }
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }

    /// <summary>
    /// Converts function arguments: field reference, literal value, or nested expression.
    /// </summary>
    public class FunctionArgumentConverter : JsonConverter<FunctionArgument>
    {
        public override FunctionArgument Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var result = new FunctionArgument();

            // Handle primitive literals (number, string, bool)
            if (reader.TokenType == JsonTokenType.Number || reader.TokenType == JsonTokenType.String || reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
            {
                using var doc1 = JsonDocument.ParseValue(ref reader);
                result.Literal = doc1.RootElement.Clone();
                return result;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Function argument must be primitive value or object.");

            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            if (root.TryGetProperty("type", out _))
            {
                result.Expression = JsonSerializer.Deserialize<ComputedExpressionBase>(root.GetRawText(), options);
            }
            else if (root.TryGetProperty("field", out var fieldProp))
            {
                result.FieldRef = new FieldReference { Field = fieldProp.GetString() ?? string.Empty };
            }
            else
            {
                throw new JsonException("Function argument object must have 'type' (expression) or 'field' (field reference).");
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, FunctionArgument value, JsonSerializerOptions options)
        {
            if (value.Literal.HasValue)
            {
                value.Literal.Value.WriteTo(writer);
            }
            else if (value.FieldRef != null)
            {
                JsonSerializer.Serialize(writer, value.FieldRef, options);
            }
            else if (value.Expression != null)
            {
                JsonSerializer.Serialize(writer, value.Expression, options);
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}
