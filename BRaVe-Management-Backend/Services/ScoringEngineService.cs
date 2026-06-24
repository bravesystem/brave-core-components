using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using BRaVe_Management_Backend.Models.es;
using BRaVe_Management_Backend.Services.Mock;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Options;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BRaVe_Management_Backend.Services
{
    public class ScoringEngineService : IScoringEngine
    {
        public readonly IIndicatorRepositoryService _indicatorService;
        private readonly ILogger<ScoringEngineService> _logger;
        public ScoringEngineService(IIndicatorRepositoryService indicatorService, ILogger<ScoringEngineService> logger )
        {
            _indicatorService = indicatorService;
            _logger = logger;
        }
        public async Task<List<ScoredResult>> SearchAsync(List<Household_Document> households, RuleDefinition ruleDefinition)
        {
            if (households == null || households.Count == 0)
                return new List<ScoredResult>();

            if (ruleDefinition?.Rules == null)
                return households.Select(h => new ScoredResult { DocumentId = h.HouseholdId }).ToList();

            var results = new List<ScoredResult>();

            Dictionary<string, ComputedExpressionDefinition> calcFields = new Dictionary<string, ComputedExpressionDefinition>();

            //Get All computed fields
            foreach (var rule in ruleDefinition.Rules)
            {
                foreach (var kv in await GetComputedDefinitionsAsync(rule.Criteria))
                {
                    calcFields[kv.Key] = kv.Value;
                }
            }

            //Enrich documents
            ComputedExpressionEnricher.Enrich(households, calcFields);  

            //run scoring
            foreach (var doc in households)
            {
                double score = 0;

                //if (doc.HouseholdId.Equals("SOBA0100044", StringComparison.CurrentCultureIgnoreCase))
                //{
                //    var test = true;
                //    test = false;
                //}

                foreach (var rule in ruleDefinition.Rules)
                {
                    if (EvaluateCriteria( rule.Criteria, doc))
                        score += rule.Weight;
                }

                results.Add(new ScoredResult
                {
                    DocumentId = doc.HouseholdId,
                    Score = score
                });
            }

            return AssignRanks(results);



        }

        private bool EvaluateCriteria( CriteriaNode node, Household_Document doc)
        {

            if (!string.IsNullOrEmpty(node.Operator) && node.Rules != null)
                return EvaluateComposite(node, doc);

            if (!string.IsNullOrEmpty(node.FieldName) && !string.IsNullOrEmpty(node.Op))
                return EvaluateLeaf(node, doc);

            return false;
        }

        private bool EvaluateComposite( CriteriaNode node, Household_Document doc)
        {
            if (node.Rules == null || node.Rules.Count == 0)
                return false;

            var op = (node.Operator ?? "").ToUpperInvariant().Trim();

            if (op == "AND")
            {
                return node.Rules.All(r => EvaluateCriteria(r, doc));
            }
            if (op == "OR")
            {
                return node.Rules.Any(r => EvaluateCriteria(r, doc));
            }

            return false;
        }

        private bool EvaluateLeaf( CriteriaNode node, Household_Document doc)
        {
            var fieldName = node.FieldName;
            var op = node.Op;
            var value = node.Value;
            var fieldType = node.FieldType ?? "string";

            if (string.IsNullOrEmpty(fieldName) || string.IsNullOrEmpty(op))
                return false;

            var documentValues = GetFieldValues(doc, fieldName);
            if (documentValues.Count == 0)
                return false;

            var criterionValue = GetCriterionValue(value, fieldType);

            return documentValues.Any(dv => Compare(dv, criterionValue, op, fieldType));
        }


        private async Task<Dictionary<string, ComputedExpressionDefinition>> GetComputedDefinitionsAsync( CriteriaNode criteria)
        {
            Dictionary<string, ComputedExpressionDefinition> calcFields = new Dictionary<string, ComputedExpressionDefinition>();

            if (criteria == null)
                return calcFields;

            Regex CalcFieldPattern = new Regex(@"^calc_(\d+)_(hh|ind)_.+$", RegexOptions.IgnoreCase);

            var referencedFields = CriteriaFieldExtractor.GetReferencedFields(criteria);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };


            foreach (var fieldName in referencedFields)
            {
                if (string.IsNullOrWhiteSpace(fieldName)) continue;

                var match = CalcFieldPattern.Match(fieldName);
                if (!match.Success) continue;

                if (calcFields.ContainsKey(fieldName))
                    continue;

                if (!int.TryParse(match.Groups[1].Value, out var indicatorId))
                    continue;

                try
                {
                    var indicator = await _indicatorService.GetByIdAsync(indicatorId);
                    
                    if (indicator?.JsonRule == null || string.IsNullOrWhiteSpace(indicator.JsonRule))
                        continue;

                    var definition = JsonSerializer.Deserialize<ComputedExpressionDefinition>(indicator.JsonRule, options);
                    definition.Level = indicator.Level;
                    
                    if (definition?.Expression != null && !calcFields.ContainsKey(fieldName))
                        calcFields[fieldName] = definition;

                }
                catch(Exception ex)
                {
                    // Skip Invalid JsonRule
                    _logger.LogError("Invalid JsonRule. Cannot deserialize {JsonRule}");

                }
  
            }

            return calcFields;

        }

        private List<object?> GetFieldValues(Household_Document doc, string fieldName)
        {
            //var name = fieldName.ToLowerInvariant().Replace("_", "");
            //var values = new List<object?>();

            var values = new List<object?>();
            if (string.IsNullOrEmpty(fieldName)) return values;

            if (doc.HouseholdId == "SOBA0900003")
            {
                Dummy.DoNothing();
            }

            // Survey question: survey_{surveyId}_q_{questionId}
            var surveyMatch = Regex.Match(fieldName, @"^survey_(\d+)_q_(\d+)$", RegexOptions.IgnoreCase);
            if (surveyMatch.Success)
            {
                var surveyId = int.Parse(surveyMatch.Groups[1].Value);
                var questionId = int.Parse(surveyMatch.Groups[2].Value);

                //look into all household surveys
                foreach (var survey in doc.Surveys ?? Enumerable.Empty<Survey_Document>())
                {
                    if (survey.Id != surveyId) continue;
                    foreach (var answer in survey.Answers ?? Enumerable.Empty<Question_Answer_Document>())
                    {
                        if (answer.QuestionId == questionId)
                            values.Add(answer.Answer);
                    }
                }

                //look into all member surveys
                foreach (var member in doc.Members ?? Enumerable.Empty<Member_Document>())
                {
                    foreach (var survey in member.Surveys ?? Enumerable.Empty<Survey_Document>())
                    {
                        if (survey.Id != surveyId) continue;
                        foreach (var answer in survey.Answers ?? Enumerable.Empty<Question_Answer_Document>())
                        {
                            if (answer.QuestionId == questionId)
                                values.Add(answer.Answer);
                        }
                    }
                }
                return values;
            }

            var name = fieldName.ToLowerInvariant().Replace("_", "");

            Regex CalcFieldIndPattern = new Regex(@"^calc_(\d+)_ind_.+$", RegexOptions.IgnoreCase);

            switch (name)
            {
                case "householdsize":
                    values.Add(doc.HouseholdSize);
                    break;
                case "residencestatus":
                    values.Add(doc.ResidenceStatus);
                    break;
                case "recipienttype":
                    values.Add(doc.RecipientType);
                    break;
                case "householdid":
                    values.Add(doc.HouseholdId);
                    break;
                case "householdlocation":
                    values.Add(doc.HouseholdLocation);
                    break;
                case "address":
                    values.Add(doc.Address);
                    break;
                case "tenantid":
                    values.Add(doc.TenantId);
                    break;
                case "gender":
                    foreach (var m in doc.Members ?? Enumerable.Empty<Member_Document>())
                        values.Add(m.Gender);
                    break;
                case "ageinyears":
                    foreach (var m in doc.Members ?? Enumerable.Empty<Member_Document>())
                        values.Add(m.CurrentAge.Years);
                    break;
                case "ageinmonths":
                    foreach (var m in doc.Members ?? Enumerable.Empty<Member_Document>())
                        values.Add(m.CurrentAge.Months);
                    break;
                case "ageindays":
                    foreach (var m in doc.Members ?? Enumerable.Empty<Member_Document>())
                        values.Add(m.CurrentAge.Days);
                    break;
                case "fullname":
                    foreach (var m in doc.Members ?? Enumerable.Empty<Member_Document>())
                        values.Add(m.FullName);
                    break;
                case "firstname":
                    foreach (var m in doc.Members ?? Enumerable.Empty<Member_Document>())
                        values.Add(m.FirstName);
                    break;
                case "lastname":
                    foreach (var m in doc.Members ?? Enumerable.Empty<Member_Document>())
                        values.Add(m.LastName);
                    break;
                case "middlename":
                    foreach (var m in doc.Members ?? Enumerable.Empty<Member_Document>())
                        values.Add(m.MiddleName);
                    break;
                case "householdrole":
                    foreach (var m in doc.Members ?? Enumerable.Empty<Member_Document>())
                        values.Add(m.HouseholdRole);
                    break;
                case "biometriccollected":
                    foreach (var m in doc.Members ?? Enumerable.Empty<Member_Document>())
                        values.Add(m.BiometricCollected);
                    break;
                case "photocollected":
                    foreach (var m in doc.Members ?? Enumerable.Empty<Member_Document>())
                        values.Add(m.PhotoCollected);
                    break;
                case "isduplicate":
                    foreach (var m in doc.Members ?? Enumerable.Empty<Member_Document>())
                        values.Add(m.IsDuplicate);
                    break;
                default:
                    if (doc.ComputedValues != null && doc.ComputedValues.TryGetValue(fieldName, out var hv))
                        values.Add(hv);

                    var match = CalcFieldIndPattern.Match(fieldName);

                    if (match.Success)
                    {
                        foreach (var m in doc.Members ?? Enumerable.Empty<Member_Document>())
                        {
                            if (m.ComputedValues != null && m.ComputedValues.TryGetValue(fieldName, out var mv))
                                values.Add(mv);
                        }
                    }
                    else 
                    {
                        if (doc.ComputedValues != null && doc.ComputedValues.TryGetValue(fieldName, out var mv))
                            values.Add(mv);
                    }

                    break;
            }

            return values;
        }

        private static object? GetCriterionValue(JsonElement? value, string fieldType)
        {
            /*if (!value.HasValue || value.Value.ValueKind == JsonValueKind.Null || value.Value.ValueKind == JsonValueKind.Undefined)
                return null;

            var v = value.Value;
            var type = (fieldType ?? "string").ToLowerInvariant();

            return type switch
            {
                "int" or "integer" => v.TryGetInt32(out var i) ? i : (v.TryGetInt64(out var l) ? (int)l : 0),
                "long" => v.TryGetInt64(out var l) ? l : v.TryGetInt32(out var i) ? (long)i : 0L,
                "double" => v.TryGetDouble(out var d) ? d : (v.TryGetInt32(out var i) ? (double)i : 0),
                "bool" or "boolean" => v.ValueKind == JsonValueKind.True || (v.ValueKind == JsonValueKind.String && v.GetString()?.ToLower() == "true"),
                "datetime" or "date" => v.ValueKind == JsonValueKind.String && DateTime.TryParse(v.GetString(), out var dt) ? dt : null,
                _ => v.ValueKind == JsonValueKind.Number ? v.GetRawText() : v.GetString()
            };*/

            if (!value.HasValue)
                return null;
            var v = value.Value;
            if (v.ValueKind == JsonValueKind.Null || v.ValueKind == JsonValueKind.Undefined)
                return null;

            var type = (fieldType ?? "string").ToLowerInvariant();

            return type switch
            {
                "int" or "integer" => v.TryGetInt32(out var i) ? i : (v.TryGetInt64(out var l) ? (int)l : 0),
                "long" => v.TryGetInt64(out var l) ? l : v.TryGetInt32(out var i) ? (long)i : 0L,
                "double" => v.TryGetDouble(out var d) ? d : (v.TryGetInt32(out var i) ? (double)i : 0),
                "bool" or "boolean" => v.ValueKind == JsonValueKind.True || (v.ValueKind == JsonValueKind.String && v.GetString()?.ToLower() == "true"),
                "datetime" or "date" => v.ValueKind == JsonValueKind.String && DateTime.TryParse(v.GetString(), out var dt) ? dt : null,
                _ => v.ValueKind == JsonValueKind.Number ? v.GetRawText() : v.GetString()
            };
        }

        private static bool Compare(object? docValue, object? criterionValue, string op, string fieldType)
        {
            var opLower = op.ToLowerInvariant();

            if (docValue == null || criterionValue == null)
                return opLower == "eq" && docValue == criterionValue;

            try
            {
                return fieldType?.ToLowerInvariant() switch
                {
                    "int" or "integer" or "long" => CompareNumeric(ToLong(docValue), ToLong(criterionValue), opLower),
                    "double" => CompareNumeric(ToDouble(docValue), ToDouble(criterionValue), opLower),
                    "bool" or "boolean" => CompareBool(ToBool(docValue), ToBool(criterionValue), opLower),
                    "datetime" or "date" => CompareDateTime(ToDateTime(docValue), ToDateTime(criterionValue), opLower),
                    _ => CompareString(ToString(docValue), ToString(criterionValue), opLower)
                };
            }
            catch
            {
                return false;
            }
        }

        private static bool CompareNumeric(long a, long b, string op) => op switch
        {
            "eq" => a == b,
            "ne" => a != b,
            "gt" => a > b,
            "gte" => a >= b,
            "lt" => a < b,
            "lte" => a <= b,
            _ => false
        };

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

        private static bool CompareString(string a, string b, string op)
        {
            /*var comparison = string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
            return op switch
            {
                "eq" => comparison == 0,
                "ne" => comparison != 0,
                "gt" => comparison > 0,
                "gte" => comparison >= 0,
                "lt" => comparison < 0,
                "lte" => comparison <= 0,
                "contains" => a?.Contains(b ?? "", StringComparison.OrdinalIgnoreCase) ?? false,
                "startswith" => a?.StartsWith(b ?? "", StringComparison.OrdinalIgnoreCase) ?? false,
                "endswith" => a?.EndsWith(b ?? "", StringComparison.OrdinalIgnoreCase) ?? false,
                _ => false
            };*/


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
                "startswith" => aNorm.StartsWith(bNorm),
                "endswith" => aNorm.EndsWith(bNorm),
                "like" => Like(aNorm, bNorm),
                "fuzzy" => FuzzyMatch(aNorm, bNorm),
                _ => false
            };
        }

        private static bool Like(string text, string pattern)
        {
            return LikeImpl(text, 0, pattern, 0);
        }

        private static bool LikeImpl(string text, int ti, string pattern, int pi)
        {
            while (pi < pattern.Length)
            {
                var c = pattern[pi];
                if (c == '%')
                {
                    if (pi == pattern.Length - 1) return true;
                    for (var k = ti; k <= text.Length; k++)
                    {
                        if (LikeImpl(text, k, pattern, pi + 1)) return true;
                    }
                    return false;
                }
                if (c == '_')
                {
                    if (ti >= text.Length) return false;
                    ti++; pi++;
                    continue;
                }
                if (ti >= text.Length || char.ToLowerInvariant(text[ti]) != char.ToLowerInvariant(c))
                    return false;
                ti++; pi++;
            }
            return ti == text.Length;
        }

        /// <summary>Fuzzy match using Levenshtein distance. Match if similarity >= 0.7.</summary>
        private static bool FuzzyMatch(string a, string b)
        {
            if (a.Length == 0 && b.Length == 0) return true;
            if (a.Length == 0 || b.Length == 0) return false;
            var dist = LevenshteinDistance(a, b);
            var maxLen = Math.Max(a.Length, b.Length);
            var similarity = 1.0 - ((double)dist / maxLen);
            return similarity >= 0.7;
        }

        private static int LevenshteinDistance(string a, string b)
        {
            var m = a.Length;
            var n = b.Length;
            var d = new int[m + 1, n + 1];
            for (var i = 0; i <= m; i++) d[i, 0] = i;
            for (var j = 0; j <= n; j++) d[0, j] = j;
            for (var j = 1; j <= n; j++)
                for (var i = 1; i <= m; i++)
                {
                    var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            return d[m, n];
        }

        private static bool CompareBool(bool a, bool b, string op) => op switch
        {
            "eq" => a == b,
            "ne" => a != b,
            _ => false
        };

        private static bool CompareDateTime(DateTime? a, DateTime? b, string op)
        {
            if (a == null || b == null) return op == "eq" && a == b;
            var comp = a.Value.CompareTo(b.Value);
            return op switch
            {
                "eq" => comp == 0,
                "ne" => comp != 0,
                "gt" => comp > 0,
                "gte" => comp >= 0,
                "lt" => comp < 0,
                "lte" => comp <= 0,
                _ => false
            };
        }

        private static long ToLong(object? o)
        {
            if (o == null) return 0;
            if (o is int i) return i;
            if (o is long l) return l;
            if (o is double d) return (long)d;
            if (o is string s && long.TryParse(s, out var parsed)) return parsed;
            return 0;
        }

        private static double ToDouble(object? o)
        {
            if (o == null) return 0;
            if (o is double d) return d;
            if (o is int i) return i;
            if (o is long l) return l;
            if (o is string s && double.TryParse(s, out var parsed)) return parsed;
            return 0;
        }

        private static bool ToBool(object? o)
        {
            if (o == null) return false;
            if (o is bool b) return b;
            if (o is string s) return s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "1";
            return false;
        }

        private static DateTime? ToDateTime(object? o)
        {
            if (o == null) return null;
            if (o is DateTime dt) return dt;
            if (o is string s && DateTime.TryParse(s, out var parsed)) return parsed;
            return null;
        }

        private static string ToString(object? o) => o?.ToString() ?? "";

        private static List<ScoredResult> AssignRanks(List<ScoredResult> results)
        {
            var ordered = results.OrderByDescending(r => r.Score).ToList();
            int rank = 0;
            int denseRank = 0;
            double? prevScore = null;

            for (int i = 0; i < ordered.Count; i++)
            {
                var r = ordered[i];
                r.RowNumber = i + 1;

                if (prevScore == null || Math.Abs(r.Score - prevScore.Value) > 0.0001)
                {
                    rank = i + 1;
                    denseRank++;
                    prevScore = r.Score;
                }
                r.Rank = rank;
                r.DenseRank = denseRank;
            }

            return ordered;
        }

      
    }
}
