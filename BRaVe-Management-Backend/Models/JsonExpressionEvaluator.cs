using BRaVe_Management_Backend.Models.es;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BRaVe_Management_Backend.Models
{
    public static class JsonExpressionEvaluator
    {
        public static object? Evaluate(ComputedExpressionBase expr, Household_Document doc, Member_Document? member)
        {
            return expr switch
            {
                TernaryExpression t => EvaluateTernary(t, doc, member),
                ArithmeticExpression a => EvaluateArithmetic(a, doc, member),
                SwitchExpression s => EvaluateSwitch(s, doc, member),
                ComparisonExpression c => EvaluateComparison(c, doc, member),
                FunctionExpression c => EvaluateFunction(c, doc, member),
                _ => null
            };
        }

        /*private static object? EvaluateFunction(FunctionExpression f, Household_Document doc, Member_Document? member)
        {
            var name = (f.Name ?? "").Trim().ToUpperInvariant();
            return name switch
            {
                "NOT" => EvaluateNot(f, doc, member),
                _ => null
            };
        }*/

        private static object? EvaluateFunction(FunctionExpression f, Household_Document doc, Member_Document? member)
        {
            var name = (f.Name ?? "").Trim().ToUpperInvariant();
            return name switch
            {
                "NOT" => EvaluateNot(f, doc, member),
                // Logical chains from ComputedExpressionParserImproved (TryParseLogicalComparison).
                "AND" => EvaluateAnd(f, doc, member),
                "OR" => EvaluateOr(f, doc, member),
                _ => null
            };
        }

        private static object? EvaluateAnd(FunctionExpression f, Household_Document doc, Member_Document? member)
        {
            if (f.Args == null || f.Args.Count == 0)
                return null;
            foreach (var arg in f.Args)
            {
                if (!ToBool(EvaluateFunctionArgument(arg, doc, member)))
                    return false;
            }
            return true;
        }

        private static object? EvaluateOr(FunctionExpression f, Household_Document doc, Member_Document? member)
        {
            if (f.Args == null || f.Args.Count == 0)
                return null;
            foreach (var arg in f.Args)
            {
                if (ToBool(EvaluateFunctionArgument(arg, doc, member)))
                    return true;
            }
            return false;
        }


        private static object? EvaluateNot(FunctionExpression f, Household_Document doc, Member_Document? member)
        {
            if (f.Args == null || f.Args.Count == 0)
                return null;
            var arg = EvaluateFunctionArgument(f.Args[0], doc, member);
            return !ToBool(arg);
        }

        private static object? EvaluateFunctionArgument(FunctionArgument arg, Household_Document doc, Member_Document? member)
        {
            if (arg == null) return null;
            if (arg.Field != null)
                return GetSingleFieldValue(doc, arg.Field, member);
            if (arg.Expression != null)
                return Evaluate(arg.Expression, doc, member);
            if (arg.Literal.HasValue)
                return JsonToObject(arg.Literal.Value);
            return null;
        }

        private static bool ToBool(object? o)
        {
            if (o == null) return false;
            if (o is bool b) return b;
            if (o is int i) return i != 0;
            if (o is long l) return l != 0;
            if (o is double d) return Math.Abs(d) >= 0.0001;
            if (o is string s) return !string.IsNullOrWhiteSpace(s);
            return false;
        }

        private static object? EvaluateComparison(ComparisonExpression c, Household_Document doc, Member_Document? member)
        {
            var fieldValues = GetFieldValues(doc, c.Field, member);
            var criterionValue = JsonToObject(c.Value);
            return fieldValues.Any(v => Compare(v, criterionValue, c.Op ?? ""));
        }

        private static object? EvaluateTernary(TernaryExpression t, Household_Document doc, Member_Document? member)
        {
            var cond = EvaluateCondition(t.Condition, doc, member);
            return cond ? JsonToObject(t.ThenValue) : JsonToObject(t.ElseValue);
        }

        private static bool EvaluateCondition(ComparisonCondition cond, Household_Document doc, Member_Document? member)
        {
            var fieldValues = GetFieldValues(doc, cond.Field, member);
            var criterionValue = JsonToObject(cond.Value);

            return fieldValues.Any(v => Compare(v, criterionValue, cond.Op));
        }

        private static object? EvaluateArithmetic(ArithmeticExpression a, Household_Document doc, Member_Document? member)
        {
            var left = EvaluateOperand(a.Left, doc, member);
            var right = EvaluateOperand(a.Right, doc, member);

            var l = ToDouble(left);
            var r = ToDouble(right);

            return a.Op?.ToLowerInvariant() switch
            {
                "add" => l + r,
                "subtract" => l - r,
                "multiply" => l * r,
                "divide" => r == 0 ? 0 : l / r,
                _ => 0
            };
        }

        private static object? EvaluateOperand(ArithmeticOperand op, Household_Document doc, Member_Document? member)
        {
            if (op.Literal.HasValue)
                return op.Literal.Value;
            if (op.FieldRef != null)
                return GetSingleFieldValue(doc, op.FieldRef.Field, member);
            if (op.Expression != null)
                return Evaluate(op.Expression, doc, member);
            return null;
        }

        private static object? EvaluateSwitch(SwitchExpression s, Household_Document doc, Member_Document? member)
        {
            foreach (var c in s.Cases ?? Enumerable.Empty<SwitchCase>())
            {
                if (c.Condition != null && EvaluateCondition(c.Condition, doc, member))
                    return JsonToObject(c.Value);
            }
            return JsonToObject(s.Default);
        }

        static string surveyPattern = @"^survey_(\d+)_q_(\d+)$";

        /// <summary>
        /// Gets values for a field. Household-level: single value. Member-level: when member set, that member only; else all members (any-match for conditions).
        /// </summary>
        private static List<object?> GetFieldValues(Household_Document doc, string fieldName, Member_Document? member)
        {
            var values = new List<object?>();
            if (string.IsNullOrEmpty(fieldName)) return values;

            var match = Regex.Match(fieldName, surveyPattern);

            if (match.Success)
            {

                int surveyId = 0, questionId = 0;

                int.TryParse(match.Groups[1].Value, out surveyId);
                int.TryParse(match.Groups[2].Value, out questionId);

                values.Add(GetHouseholdSurveyValue(doc, surveyId, questionId));
                return values;

               // values.Add(GetMemberSurveyValue(member, surveyId, questionId));
               // return values;
            }

            var name = NormalizeFieldName(fieldName);

            if (IsHouseholdLevelField(name))
            {
                values.Add(GetHouseholdFieldValue(doc, name));
                return values;
            }

            if (member != null)
            {
                values.Add(GetMemberFieldValue(member, name));
                return values;
            }

            foreach (var m in doc.Members ?? Enumerable.Empty<Member_Document>())
                values.Add(GetMemberFieldValue(m, name));
            return values;
        }

        private static object? GetHouseholdSurveyValue(Household_Document doc, int surveyId, int questionId)
        {

            foreach (var survey in doc.Surveys ?? Enumerable.Empty<Survey_Document>())
            {
                if (survey.Id != surveyId) continue;
                foreach (var answer in survey.Answers ?? Enumerable.Empty<Question_Answer_Document>())
                {
                    if (answer.QuestionId == questionId)
                        return answer.Answer;
                }
            }

            return null;
        }

        private static object? GetMemberSurveyValue(Member_Document? m, int surveyId, int questionId)
        {
            if (m == null) 
                return null;

            foreach (var survey in m.Surveys ?? Enumerable.Empty<Survey_Document>())
            {
                if (survey.Id != surveyId) continue;
                foreach (var answer in survey.Answers ?? Enumerable.Empty<Question_Answer_Document>())
                {
                    if (answer.QuestionId == questionId)
                        return answer.Answer;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets a single value for a field. Household-level: doc value. Member-level: when member set, that member; else first member.
        /// </summary>
        private static object? GetSingleFieldValue(Household_Document doc, string fieldName, Member_Document? member)
        {
            var name = NormalizeFieldName(fieldName);

            if (IsHouseholdLevelField(name))
                return GetHouseholdFieldValue(doc, name);

            if (member != null)
                return GetMemberFieldValue(member, name);

            var first = (doc.Members ?? Enumerable.Empty<Member_Document>()).FirstOrDefault();
            return first != null ? GetMemberFieldValue(first, name) : null;
        }

        private static bool IsHouseholdLevelField(string normalizedName) => normalizedName switch
        {
            "householdsize" or "family_size" or "familysize" => true,
            "residencestatus" or "recipienttype" or "householdid" or "householdlocation" or "address" or "tenantid" => true,
            _ => false
        };

        private static object? GetHouseholdFieldValue(Household_Document doc, string name)
        {
            return name switch
            {
                "householdsize" or "family_size" or "familysize" => doc.HouseholdSize,
                "residencestatus" => doc.ResidenceStatus,
                "recipienttype" => doc.RecipientType,
                "householdid" => doc.HouseholdId,
                "householdlocation" => doc.HouseholdLocation,
                "address" => doc.Address,
                "tenantid" => doc.TenantId,
                _ => null
            };
        }

        private static object? GetMemberFieldValue(Member_Document m, string name)
        {
            return name switch
            {
                "ageinyears" or "age_in_years" => m.AgeInYears,
                "ageinmonths" or "age_in_months" => m.AgeInMonths,
                "ageindays" or "age_in_days" => m.AgeInDays,
                "gender" => m.Gender,
                "householdrole" or "household_role" => m.HouseholdRole,
                "firstname" or "first_name" => m.FirstName,
                "lastname" or "last_name" => m.LastName,
                "middlename" or "middle_name" => m.MiddleName,
                "biometriccollected" => m.BiometricCollected,
                "photocollected" => m.PhotoCollected,
                _ => null
            };
        }

        private static string NormalizeFieldName(string fieldName) =>
            fieldName?.ToLowerInvariant().Replace("_", "") ?? "";

        private static object? JsonToObject(JsonElement el)
        {
            switch (el.ValueKind)
            {
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return null;
                case JsonValueKind.Number:
                    if (el.TryGetInt32(out var i)) return i;
                    if (el.TryGetInt64(out var l)) return l;
                    return el.TryGetDouble(out var d) ? d : 0;
                case JsonValueKind.String:
                    return el.GetString();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                default:
                    return el.GetRawText();
            }
        }

        private static double ToDouble(object? o)
        {
            if (o == null) return 0;
            if (o is int i) return i;
            if (o is long l) return l;
            if (o is double d) return d;
            if (o is float f) return f;
            return double.TryParse(o.ToString(), out var parsed) ? parsed : 0;
        }

        private static bool Compare(object? docValue, object? criterionValue, string op)
        {
            var opLower = (op ?? "").ToLowerInvariant();
            if (docValue == null || criterionValue == null)
                return opLower == "eq" && docValue == criterionValue;

            if (docValue is string || criterionValue is string)
                return CompareString(ToString(docValue), ToString(criterionValue), opLower);

            if (IsNumeric(docValue) || IsNumeric(criterionValue))
                return CompareNumeric(ToDouble(docValue), ToDouble(criterionValue), opLower);

            if (docValue is bool b1 && criterionValue is bool b2)
                return CompareBool(b1, b2, opLower);

            return CompareString(ToString(docValue), ToString(criterionValue), opLower);
        }

        private static bool IsNumeric(object? o) =>
            o is int or long or double or float or decimal;

        private static bool CompareNumeric(double a, double b, string op) => op switch
        {
            "eq" => Math.Abs(a - b) < 0.0001,
            "ne" => Math.Abs(a - b) >= 0.0001,
            "gt" => a > b,
            "gte" => a >= b,
            "lt" => a < b,
            "lte" => a <= b,
            _ => false
        };

        private static bool CompareBool(bool a, bool b, string op) => op switch
        {
            "eq" => a == b,
            "ne" => a != b,
            _ => false
        };

        private static bool CompareString(string a, string b, string op)
        {
            a ??= "";
            b ??= "";
            var aNorm = a.ToLowerInvariant();
            var bNorm = b.ToLowerInvariant();
            var comparison = string.Compare(aNorm, bNorm, StringComparison.Ordinal);
            return op switch
            {
                "eq" => comparison == 0,
                "ne" => comparison != 0,
                "gt" => comparison > 0,
                "gte" => comparison >= 0,
                "lt" => comparison < 0,
                "lte" => comparison <= 0,
                "contains" => aNorm.Contains(bNorm),
                "startswith" => aNorm.StartsWith(bNorm, StringComparison.Ordinal),
                "endswith" => aNorm.EndsWith(bNorm, StringComparison.Ordinal),
                "like" => Like(aNorm, bNorm),
                "fuzzy" => Fuzzy(aNorm, bNorm),
                _ => false
            };
        }

        /// <summary>
        /// Fuzzy match: true if text contains pattern, or if similarity (1 - normalized Levenshtein) is at least 0.6.
        /// </summary>
        private static bool Fuzzy(string aNorm, string bNorm)
        {
            if (string.IsNullOrEmpty(bNorm)) return true;
            if (string.IsNullOrEmpty(aNorm)) return false;
            if (aNorm.Contains(bNorm)) return true;
            var similarity = 1.0 - (double)LevenshteinDistance(aNorm, bNorm) / Math.Max(aNorm.Length, bNorm.Length);
            return similarity >= 0.6;
        }

        private static int LevenshteinDistance(string a, string b)
        {
            var m = a.Length;
            var n = b.Length;
            var d = new int[m + 1, n + 1];
            for (var i = 0; i <= m; i++) d[i, 0] = i;
            for (var j = 0; j <= n; j++) d[0, j] = j;
            for (var j = 1; j <= n; j++)
            {
                for (var i = 1; i <= m; i++)
                {
                    var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }
            return d[m, n];
        }

        private static bool Like(string text, string pattern)
        {
            if (string.IsNullOrEmpty(pattern)) return true;
            var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
                .Replace("%", ".*").Replace("_", ".") + "$";
            return System.Text.RegularExpressions.Regex.IsMatch(text ?? "", regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        private static string ToString(object? o) => o?.ToString() ?? "";
    }
}
