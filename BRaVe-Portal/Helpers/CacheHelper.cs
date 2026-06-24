namespace BRaVe_Portal.Helpers
{
    public static class CacheHelper
    {
        public static string GetKey(string identifier, int suffix)
        {
            return $"rba:{identifier}:{suffix}";
        }
    }
}
