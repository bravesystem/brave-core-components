using System.Text.RegularExpressions;

namespace BRaVe_Portal.Helpers
{
    using System;
    using System.Text.RegularExpressions;

    public static class FuncRewriter
    {
        private static readonly Regex FuncCallRegex = new Regex(
            @"(?ix)
            \b
            (FUNC|FUZZY|LIKE|STARTSWITH|ENDSWITH|CONTAINS)   # group1: function name
            \s* \(                                           # opening parenthesis
                \s* (\[[^\]]+\]) \s*                         # group2: [var]
                , \s* '((?:''|[^'])*)' \s*                   # group3: value ('' allowed)
            \)                                               # closing parenthesis
        ",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        public static string RewriteMatches(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return FuncCallRegex.Replace(input, match =>
            {
                var func = match.Groups[1].Value.ToLowerInvariant();
                var variable = match.Groups[2].Value;
                var value = match.Groups[3].Value;

                return $"{variable} {func} '{value}'";
            });
        }
    }

}
