using BRaVe_Portal.Models.DTOs;

namespace BRaVe_Portal.Helpers
{
    public static class LookupHelper
    {

        // Returns null if not found
        public static string? GetTextById(this IEnumerable<LookupItemDto> items, int id)
            => items?.FirstOrDefault(x => x.Id == id)?.Text;

        // Returns a fallback if not found (empty by default)
        public static string GetTextByIdOr(this IEnumerable<LookupItemDto> items, int id, string fallback = "")
            => items?.FirstOrDefault(x => x.Id == id)?.Text ?? fallback;

    }
}
