namespace BRaVe_Portal.Helpers
{
    public class AlgoUtils
    {


        public static bool SimilarNames(string strA, string strB, double threshold=0.7)
        {
            return FuzzyMatch(strA, strB, threshold);
        }

        private static bool FuzzyMatch(string a, string b, double minSimilarity)
        {
            a ??= "";
            b ??= "";
            if (a.Length == 0 && b.Length == 0) return true;
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


    }
}
