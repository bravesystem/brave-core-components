using System.Text.RegularExpressions;

namespace BRaVe_Portal.Helpers
{
    public class StringHelper
    {

        // <summary>
        /// Returns a shortened version of the input text.
        /// If the text length exceeds maxLength, it returns the first maxLength characters followed by "...".
        /// </summary>
        /// <param name="text">The input string.</param>
        /// <param name="maxLength">Maximum allowed characters before truncation (default: 250).</param>
        /// <returns>Truncated string with ellipsis if needed.</returns>
        public static string TruncateWithEllipsis(string text, int maxLength = 250)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return text.Length <= maxLength
                ? text
                : text.Substring(0, maxLength) + "...";
        }


        public static bool IsPotentialPathProbe(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var input = Uri.UnescapeDataString(value).Trim();

            return
                // File/stream URI schemes
                Regex.IsMatch(input, @"^(file|php|zip|data|glob|expect):",
                              RegexOptions.IgnoreCase)

                // Absolute Unix paths
                || input.StartsWith("/", StringComparison.OrdinalIgnoreCase)

                // Absolute Windows paths (C:\)
                || Regex.IsMatch(input, @"^[a-z]:\\", RegexOptions.IgnoreCase)

                // Windows UNC paths (\\server\share)
                || input.StartsWith(@"\\", StringComparison.OrdinalIgnoreCase)

                // Directory traversal
                || input.Contains("../", StringComparison.OrdinalIgnoreCase)
                || input.Contains("..\\", StringComparison.OrdinalIgnoreCase);
        }


    }
}
