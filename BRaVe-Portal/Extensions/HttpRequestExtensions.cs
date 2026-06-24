using Azure.Core;
using BRaVe_Portal.Helpers;

namespace BRaVe_Portal.Extensions
{

    public static class HttpRequestExtensions
    {

        public static bool ContainsUnexpectedQueryParameters(this HttpRequest request)
        {
            // Datasets Index GET does not require query parameters.
            return request.Query.Count > 0;
        }



        public static bool ContainsPathProbeInQuery(this HttpRequest request)
        {
            return request.Query.Count > 0 &&
                   request.Query.Any(q =>
                       StringHelper.IsPotentialPathProbe(q.Value.ToString()));
        }

        public static async Task<bool> ContainsPathProbeInFormAsync(this HttpRequest request)
        {
            if (!request.HasFormContentType)
            {
                return false;
            }

            var form = await request.ReadFormAsync();

            return form
                .SelectMany(entry => entry.Value)
                .Any(StringHelper.IsPotentialPathProbe);
        }


        public static bool TryGetOptionalPositiveIntFromQuery(
               this HttpRequest request,
                ILogger logger,
               string key,
               out int? value)
        {
            value = null;

            if (!request.Query.TryGetValue(key, out var rawValues))
            {
                // Parameter not present → valid (optional)
                return true;
            }

            var raw = rawValues.ToString();
            if (string.IsNullOrWhiteSpace(raw))
            {
                // Present but empty → treat as not supplied
                return true;
            }

            if (!int.TryParse(raw, out var parsed) || parsed <= 0)
            {
                logger.LogWarning(
                    "Blocked invalid query value for {QueryKey} on {Path}.",
                    key,
                    request.Path);

                return false;
            }

            value = parsed;
            return true;
        }

        public static bool TryGetSafeOptionalTextFromQuery(this HttpRequest request,
            ILogger logger, string key, out string? value, int maxLength = 256)
        {
            value = null;

            if (!request.Query.TryGetValue(key, out var rawValues))
            {
                return true;
            }

            var raw = rawValues.ToString();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return true;
            }

            if (raw.Length > maxLength || raw.Any(char.IsControl))
            {
                logger.LogWarning("Blocked invalid text query value for {QueryKey} on {Path}.", 
                    key, 
                    request.Path);

                return false;
            }

            value = raw;
            return true;
        }

    }
}
