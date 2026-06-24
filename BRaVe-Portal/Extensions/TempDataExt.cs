using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace BRaVe_Portal.Extensions
{
    public static class TempDataExt
    {
        public static void Put<T>(this ITempDataDictionary t, string k, T v)
            => t[k] = System.Text.Json.JsonSerializer.Serialize(v);
        public static T? Get<T>(this ITempDataDictionary t, string k)
            => t.TryGetValue(k, out var o) ? System.Text.Json.JsonSerializer.Deserialize<T>((string)o!) : default;
    }
}
