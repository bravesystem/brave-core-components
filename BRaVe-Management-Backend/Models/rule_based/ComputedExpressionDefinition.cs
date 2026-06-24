using BRaVe_Management_Backend.DTOs;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BRaVe_Management_Backend.Models
{
    public class ComputedExpressionDefinition
    {
        [JsonIgnore]
        public IndicatorLevel Level { get; set; } = IndicatorLevel.Household;

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
        public string Op { get; set; } = string.Empty; // eq, ne, lt, lte, gt, gte, contains, like

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
    /// </summary>
    public class ComparisonExpression : ComputedExpressionBase
    {
        public override string Type => "comparison";

        [JsonPropertyName("field")]
        public string Field { get; set; } = string.Empty;

        [JsonPropertyName("op")]
        public string Op { get; set; } = string.Empty; // eq, ne, lt, lte, gt, gte, contains, like, fuzzy

        [JsonPropertyName("value")]
        public JsonElement Value { get; set; }
    }

    /// <summary>
    /// Field reference expression: bare field as the whole expression (identity).
    /// </summary>
    public class FieldRefExpression : ComputedExpressionBase
    {
        public override string Type => "field";

        [JsonPropertyName("field")]
        public string Field { get; set; } = string.Empty;
    }

    /// <summary>
    /// Function expression: custom function call e.g. NOT([has_biometric]).
    /// </summary>
    public class FunctionExpression : ComputedExpressionBase
    {
        public override string Type => "function";

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("args")]
        public List<FunctionArgument> Args { get; set; } = new();
    }

    /// <summary>
    /// Argument in a function call: field reference, literal, or nested expression.
    /// </summary>
    [JsonConverter(typeof(FunctionArgumentConverter))]
    public class FunctionArgument
    {
        public string? Field { get; set; }
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
                "field" => JsonSerializer.Deserialize<FieldRefExpression>(root.GetRawText(), options),
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
    /// Converts function arguments: field reference { "field": "x" }, or nested expression { "type": "...", ... }, or literal.
    /// </summary>
    public class FunctionArgumentConverter : JsonConverter<FunctionArgument>
    {
        public override FunctionArgument Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var result = new FunctionArgument();
            if (reader.TokenType == JsonTokenType.Number || reader.TokenType == JsonTokenType.String ||
                reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                result.Literal = doc.RootElement.Clone();
                return result;
            }
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Function argument must be object or literal.");
            using var doc2 = JsonDocument.ParseValue(ref reader);
            var root = doc2.RootElement;
            if (root.TryGetProperty("type", out _))
            {
                result.Expression = JsonSerializer.Deserialize<ComputedExpressionBase>(root.GetRawText(), options);
            }
            else if (root.TryGetProperty("field", out var fieldProp))
            {
                result.Field = fieldProp.GetString();
            }
            else
            {
                result.Literal = root.Clone();
            }
            return result;
        }

        public override void Write(Utf8JsonWriter writer, FunctionArgument value, JsonSerializerOptions options)
        {
            if (value.Literal.HasValue)
            {
                value.Literal.Value.WriteTo(writer);
            }
            else if (value.Field != null)
            {
                writer.WriteStartObject();
                writer.WriteString("field", value.Field);
                writer.WriteEndObject();
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
