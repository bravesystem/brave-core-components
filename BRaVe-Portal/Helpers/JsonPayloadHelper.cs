using System.Text.Json;

namespace BRaVe_Portal.Helpers
{
    public class JsonPayloadHelper
    {
        public static bool ContainsPathProbe(object? payload)
        {
            if (payload == null)
            {
                return false;
            }

            var serialized = JsonSerializer.Serialize(payload);
            return StringHelper.IsPotentialPathProbe(serialized);
        }
    }
}
