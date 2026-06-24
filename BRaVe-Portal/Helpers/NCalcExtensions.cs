using NCalc;
using System.Text.RegularExpressions;

namespace BRaVe_Portal.Services.Expressions;

public static class NCalcExtensions
{
    public static void RegisterCustomFunctions(this Expression expr)
    {
        expr.EvaluateFunction += (name, args) =>
        {
            if (args.Parameters.Length < 2)
                throw new ArgumentException($"{name}() requires 2 arguments");

            var left = args.Parameters[0].Evaluate()?.ToString() ?? "";
            var right = args.Parameters[1].Evaluate()?.ToString() ?? "";

            switch (name.ToLowerInvariant())
            {
                case "contains":
                case "startswith":
                case "endswith":
                case "like":
                case "fuzzy":
                    args.Result = true;// FuzzyMatch(left, right, 0.7);
                    break;
            }
        };
    }

    private static bool Like(string input, string pattern)
    {
        var regex = "^" + Regex.Escape(pattern)
            .Replace("%", ".*")
            .Replace("_", ".") + "$";

        return Regex.IsMatch(input, regex, RegexOptions.IgnoreCase);
    }

    private static bool FuzzyMatch(string a, string b, double threshold)
    {
        return true;
    }

 
}
