using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using BRaVe_Management_Backend.Models.es;
using System;

namespace BRaVe_Management_Backend.Services
{
    public class DuplicateScoringEngine : IDuplicateScoringEngine
    {
        public async Task<DuplicateMatchResultsViewModel> FindDuplicates(List<Household_Document> households, DuplicateCriteriaDefinition criteria)
        {
            var results = new DuplicateMatchResultsViewModel();
            
            if (households == null || criteria?.Rules == null || criteria.Rules.Count == 0)
                return results;

            var hhList = households.Where(h => h?.Members != null).ToList();

            double maxScore = 0;
            double minScore = double.MaxValue;

            for (var i = 0; i < hhList.Count; i++)
            {
                for (var j = i + 1; j < hhList.Count; j++)
                {
                    var hhA = hhList[i];
                    var hhB = hhList[j];

                    if (hhA.HouseholdId == hhB.HouseholdId)
                            continue; //likely not happening

                    var pairs = GetUniquePairs(hhA.Members, hhB.Members);

                    foreach (var (memberA, memberB) in pairs)
                    {

                        var (ma, mb, ha, hb) = OrderByRegisteredOn(memberA, memberB, hhA, hhB);

                        if (ma.IsDedupChecked) //if already checked, discard
                            continue;

                        var score = EvaluateScore(ma, ha, mb, hb, criteria);

                        int cnt = results.Records.Count;

                        AddIfNotExists(results, BuildResult(ma, mb, ha, hb, score));

                        if (results.Records.Count > cnt)
                        {

                            if (score > maxScore)
                                maxScore = score;

                            if (score < minScore)
                                minScore = score;

                        }

                    }

                    /*foreach (var memberA in hhA.Members ?? Enumerable.Empty<Member_Document>())
                    {
                        if (memberA.IsDuplicate)
                            continue;

                        foreach (var memberB in hhB.Members ?? Enumerable.Empty<Member_Document>())
                        {
                            if (memberB.IsDuplicate)
                                continue; 

                            if (memberA.MemberId == memberB.MemberId) 
                                continue; //likely not happening

                            var (ma, mb, ha, hb) = OrderByRegisteredOn(memberA, memberB, hhA, hhB);

                            if (ma.IsDedupChecked) //if already checked, discard
                            {
                                discard.Add(ma.MemberId);

                            }    

                            var score = EvaluateScore(ma, ha, mb, hb, criteria);

                            int cnt = results.Records.Count;

                            AddIfNotExists(results, BuildResult(ma, mb, ha, hb, score));

                            if (results.Records.Count > cnt)
                            {

                                if (score > maxScore)
                                    maxScore = score;

                                if (score < minScore)
                                    minScore = score;

                            }
                        }
                    }*/
                }
            }

            var filtered = new List<DuplicateMatchResult>();

            double Threshold = 0.75;

            foreach (var r in results.Records)
            {
                if (r.TotalScore >0)// maxScore*Threshold)
                    filtered.Add(r);
            }

            //if (minScore > maxScore * Threshold)
            //    minScore = maxScore * Threshold;

            results.Records = filtered;
            results.MinScore = minScore;
            results.MaxScore = maxScore;

            return results;    
        }

        public void AddIfNotExists(DuplicateMatchResultsViewModel result, DuplicateMatchResult newRecord)
        {
            bool exists = result.Records.Any(r =>
                r.MemberUuidA == newRecord.MemberUuidA &&
                r.MemberUuidB == newRecord.MemberUuidB
            );

            if (!exists)
            {
                result.Records.Add(newRecord);
            }
        }


        public static List<(Member_Document A, Member_Document B)> GetUniquePairs(List<Member_Document> list1, List<Member_Document> list2)
        {
            var result = new List<(Member_Document A, Member_Document B)>();

            foreach (var a in list1)
            {
                foreach (var b in list2)
                {
                    // skip if same member
                    if (a.MemberId == b.MemberId)
                        continue;

                    if (a.IsDuplicate)
                        continue;

                    if (b.IsDuplicate)
                        continue;

                    // enforce ordering: keep only (smallerId, largerId)
                    if (a.MemberId.CompareTo(b.MemberId) < 0)
                    {
                        result.Add((a, b));
                    }
                }
            }

            return result;
        }

        private static (Member_Document memberA, Member_Document memberB, Household_Document hhA, Household_Document hhB) OrderByRegisteredOn(
        Member_Document ma, Member_Document mb, Household_Document hhA, Household_Document hhB)
        {
            var regA = ma.RegisteredOn ?? DateTime.MaxValue;
            var regB = mb.RegisteredOn ?? DateTime.MaxValue;

            if (regA < regB)
                return (mb, ma, hhB, hhA);
            
            if (regB < regA)
                return (ma, mb, hhA, hhB);

            return string.CompareOrdinal(ma.MemberId, mb.MemberId) <= 0
                ? (ma, mb, hhA, hhB)
                : (mb, ma, hhB, hhA);
        }


        private static double EvaluateScore(Member_Document memberA, Household_Document hhA, Member_Document memberB, Household_Document hhB, DuplicateCriteriaDefinition criteria)
        {
            double total = 0;
            foreach (var rule in criteria.Rules)
            {
                if (RuleMatches(memberA,hhA, memberB, hhB, rule))
                    total += rule.Score;
            }
            return total;
        }



        public async Task<Dictionary<int, PredicateEvaluation>> DuplicatePredicatesEvalResults(Member_Document memberA, Household_Document hhA, Member_Document memberB, Household_Document hhB, DuplicateCriteriaDefinition criteria)
        {
            Dictionary<int, PredicateEvaluation> result = new Dictionary<int, PredicateEvaluation>();

            foreach (var rule in criteria.Rules)
            {
                bool eval = RuleMatches(memberA, hhA, memberB, hhB, rule);

                result.Add(rule.Id.Value, new PredicateEvaluation
                {
                    rule = rule,
                    EvaluationResult = eval

                });

            }

            return result;
        }


        private static bool RuleMatches(Member_Document a, Household_Document hhA, Member_Document b, Household_Document hhB, DuplicateRule rule)
        {
            if (rule.Fields == null || rule.Fields.Count == 0) return false;

            var op = (rule.Op ?? "eq").ToLowerInvariant();

            if (op == "diff_lte" || op == "diff_lt" || op == "diff_gte")
            {
                var valA = GetMemberFieldValue(a,hhA, rule.Fields[0]);
                var valB = GetMemberFieldValue(b,hhB, rule.Fields[0]);

                var diff = 0.0;

                try
                {
                    var numA = ToDouble(valA);
                    var numB = ToDouble(valB);
                    diff = Math.Abs(numA - numB);
                }
                catch 
                {
                    return false;
                }
                
                var threshold = rule.Value ?? 0;

                return op switch
                {
                    "diff_lte" => diff <= threshold,
                    "diff_lt" => diff < threshold,
                    "diff_gte" => diff >= threshold,
                    _ => false
                };
            }

            if (op == "eq" || op == "ne")
            {
                var allMatch = true;
                foreach (var field in rule.Fields)
                {
                    var va = GetMemberFieldValue(a,hhA, field);
                    var vb = GetMemberFieldValue(b,hhB, field);

                    var match = CompareEq(va, vb); //2 nulls are not considered equal

                    if (!match) { allMatch = false; break; }
                }
                return op == "eq" ? allMatch : !allMatch;
            }

            if (op == "fuzzy")
            {
                var strA = string.Join(" ", rule.Fields.Select(f => ToString(GetMemberFieldValue(a,hhA, f)))).Trim();
                var strB = string.Join(" ", rule.Fields.Select(f => ToString(GetMemberFieldValue(b,hhB, f)))).Trim();
                var threshold = rule.Value ?? 0.7;
                return FuzzyMatch(strA, strB, threshold);
            }

            if (op == "contains")
            {
                var strA = string.Join(" ", rule.Fields.Select(f => ToString(GetMemberFieldValue(a,hhA, f)))).Trim();
                var strB = string.Join(" ", rule.Fields.Select(f => ToString(GetMemberFieldValue(b,hhB, f)))).Trim();
                return !string.IsNullOrEmpty(strA) && !string.IsNullOrEmpty(strB) &&
                       (strA.ToLowerInvariant().Contains(strB.ToLowerInvariant()) ||
                        strB.ToLowerInvariant().Contains(strA.ToLowerInvariant()));
            }

            return false;
        }

        private static bool CompareEq(object? a, object? b)
        {
            if (a == null && b == null) return false;//true;
            if (a == null || b == null) return false;

            string A = ToString(a); string B = ToString(b);

            if (A.Length == 0 && B.Length == 0) return false;
            if (A.Length == 0 || B.Length == 0) return false;

            return string.Equals(A, B, StringComparison.OrdinalIgnoreCase);
        }

        private static object? GetMemberFieldValue(Member_Document m, Household_Document hh, string fieldName)
        {
            var name = (fieldName ?? "").ToLowerInvariant().Replace("_", "");
            return name switch
            {
                "lastname" => m.LastName,
                "firstname" => m.FirstName,
                "middlename" => m.MiddleName,
                "fullname" => m.FullName,
                "gender" => m.Gender,
                "ageinyears" => m.AgeInYears,
                "ageinmonths" => m.AgeInMonths,
                "ageindays" => m.AgeInDays,
                "householdrole" => m.HouseholdRole,
                "dateofbirth" => m.DateOfBirth,
                "photocollected" => m.PhotoCollected,
                "biometriccollected" => m.BiometricCollected,
                "registeredon" => m.RegisteredOn,
                "householdsize"=> hh.HouseholdSize,
                "householdlocation" => hh.HouseholdLocation,
                "address" => hh.Address,
                "latitude" => hh.GpsCoordinates!=null?hh.GpsCoordinates.Latitude:null,
                "longitude" => hh.GpsCoordinates!=null?hh.GpsCoordinates.Longitude:null,
                _ => null
            };
        }

        private static double ToDouble(object? o)
        {
            //if (o == null) return 0; //to avoid treating null as equal
            if (o is int i) return i;
            if (o is long l) return l;
            if (o is double d) return d;
            if (o is DateTime dt) return dt.Ticks;
            if (double.TryParse(ToString(o), out var p))
                return p;

            throw new InvalidCastException($"Cannot convert object to double.");
        }

        //private static string ToString(object? o) => o?.ToString() ?? "";

        private static string ToString(object? o) => (o?.ToString() ?? "").Replace("-", "");

        private static bool FuzzyMatch(string a, string b, double minSimilarity)
        {
            a ??= "";
            b ??= "";
            if (a.Length == 0 && b.Length == 0) return false;//true;
            if (a.Length == 0 || b.Length == 0) return false;
            var dist = LevenshteinDistance(a, b);
            var maxLen = Math.Max(a.Length, b.Length);
            var similarity = 1.0 - ((double)dist / maxLen);
            return similarity >= minSimilarity;
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
                    d[i, j] = a[i - 1] == b[j - 1]
                        ? d[i - 1, j - 1]
                        : 1 + Math.Min(Math.Min(d[i - 1, j], d[i, j - 1]), d[i - 1, j - 1]);
            return d[m, n];
        }

        private static DuplicateMatchResult BuildResult(
        Member_Document ma, Member_Document mb,
        Household_Document hhA, Household_Document hhB,
        double score)
        {
            return new DuplicateMatchResult
            {
                MemberUuidA = ma.UUID,
                MemberIdA = ma.MemberId ?? "",
                DocumentIdA = ma.MemberId ?? "",
                FullnameA = $"{ma.FirstName} {ma.LastName}".Trim(),
                AgeA = ma.AgeInYears,
                GenderA = ma.Gender ?? "",
                PictureCollectedA = ma.PhotoCollected,
                BiometricCollectedA = ma.BiometricCollected,
                HouseholdIdA = hhA.HouseholdId ?? "",
                RegisteredOnA = ma.RegisteredOn,

                MemberUuidB = mb.UUID,
                MemberIdB = mb.MemberId ?? "",
                DocumentIdB = mb.MemberId ?? "",
                FullnameB = $"{mb.FirstName} {mb.LastName}".Trim(),
                AgeB = mb.AgeInYears,
                GenderB = mb.Gender ?? "",
                PictureCollectedB = mb.PhotoCollected,
                BiometricCollectedB = mb.BiometricCollected,
                HouseholdIdB = hhB.HouseholdId ?? "",
                RegisteredOnB = mb.RegisteredOn,

                TotalScore = score
            };
        }

    }
}
