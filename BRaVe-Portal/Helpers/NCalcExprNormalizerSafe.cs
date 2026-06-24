using System.Text;
using System.Text.RegularExpressions;

namespace BRaVe_Portal.Services.Expressions;

public static class NCalcExprNormalizerSafe
{
    private static readonly Regex AndRegex =
        new Regex(@"\bAND\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex OrRegex =
        new Regex(@"\bOR\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex NotRegex =
        new Regex(@"\bNOT\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TrueRegex =
        new Regex(@"\bTRUE\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex FalseRegex =
        new Regex(@"\bFALSE\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex NullRegex =
        new Regex(@"\bNULL\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);


  
    private static string LowercaseFunctions(string input)
    {
        return input.ToLowerInvariant();
    }

    private static string NormalizeLiterals(string input)
    {
        input = TrueRegex.Replace(input, "true");
        input = FalseRegex.Replace(input, "false");
        input = NullRegex.Replace(input, "null");
        return input;
    }

    /// <summary>
    /// Normalizes expression safely outside quoted strings:
    /// AND/OR/NOT → and/or/not
    /// IF(...) → if(...)
    /// TRUE/FALSE/NULL → true/false/null
    /// </summary>
    public static string NormalizeOperatorsOutsideStrings(string expr)
    {
        if (string.IsNullOrEmpty(expr))
            return expr;

        var sb = new StringBuilder(expr.Length);

        bool inSingle = false;
        bool inDouble = false;

        for (int i = 0; i < expr.Length; i++)
        {
            char c = expr[i];

            // Toggle single quote mode
            if (!inDouble && c == '\'')
            {
                sb.Append(c);
                inSingle = !inSingle;
                continue;
            }

            // Toggle double quote mode
            if (!inSingle && c == '"')
            {
                sb.Append(c);
                inDouble = !inDouble;
                continue;
            }

            // Inside string → copy as-is
            if (inSingle || inDouble)
            {
                sb.Append(c);
                continue;
            }

            // Outside string → process chunk
            int start = i;

            while (i < expr.Length && expr[i] != '\'' && expr[i] != '"')
                i++;

            string chunk = expr.Substring(start, i - start);

            chunk = AndRegex.Replace(chunk, "and");
            chunk = OrRegex.Replace(chunk, "or");
            chunk = NotRegex.Replace(chunk, "not");

            chunk = LowercaseFunctions(chunk);
            chunk = NormalizeLiterals(chunk);

            sb.Append(chunk);

            if (i < expr.Length)
                i--; // compensate for loop increment
        }

        return sb.ToString();
    }
}
