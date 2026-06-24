namespace BRaVe_Portal.Helpers
{
    public class AzureAdOptions
    {
        public string Instance { get; set; }
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string ApplicationUri { get; set; }
        public string[] Scopes { get; set; }
        public string CallbackPath { get; set; }
    }
}
