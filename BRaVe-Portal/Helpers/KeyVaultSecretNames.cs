namespace BRaVe_Portal.Helpers
{
    public static class KeyVaultSecretNames
    {
        public static class ExternalLinks
        {
            public const string PartnerAccess = "PARTNER_ACCESS";
        }
        public static class Sql
        {
            public const string PrimaryConnection = "AZURE_SQL_CONNECTIONSTRING";
        }

        public static class Logging
        {
            public const string App_Insight = "APPINSIGHTS_INSTRUMENTATIONKEY";
        }

        public static class Api
        {
            public const string Management_Api = "PORTAL_API_BASEURL";
        }

        public static class Storage
        {
            public const string RedisConnection = "CACHE_REDIS_CONNECTIONSTRING";
            public const string StorageConnection = "STORAGE_CONNECTIONSTRING";
        }

        public static class SecureStore
        {
            public const string Key_Vault = "KEY_VAULT_URL";
            public const string Pub_KeyName = "KEY_VAULT_KEY_NAME";
        }

        public static class Jwt
        {
            public const string Issuer = "JWT_ISSUER";
            public const string Enroll_Audience = "JWT_AUDIENCES";
            public const string API_BASE_URL = "JWT_API_BASE_URL";
            public const string Key = "JWT_KEY";
        }


        public static class AzureAd
        {
            public const string Be_Api_Uri = "AZURE_AD_BE_APP_URI";

            public const string Be_Client_Id = "AZURE_AD_BE_CLIENTID";
            public const string Fe_Client_Id = "AZURE_AD_FE_CLIENTID";

            public const string Be_Tenant_Id = "AZURE_AD_BE_TENANTID";
            public const string Fe_Tenant_Id = "AZURE_AD_FE_TENANTID";

            public const string Be_Client_Secret = "AZURE_AD_BE_CLIENT_SECRET";
            public const string Fe_Client_Secret = "AZURE_AD_FE_CLIENT_SECRET";

            public const string Be_Instance = "AZURE_AD_BE_INSTANCE";
            public const string Fe_Instance = "AZURE_AD_FE_INSTANCE";

            public const string Fe_Audience_Scope = "AZURE_AD_FE_SCOPE";
            public const string Be_Audience_Scope = "AZURE_AD_BE_SCOPE";

            public const string Fe_Callback = "AZURE_AD_FE_CALLBACK";
            public const string Be_Callback = "AZURE_AD_BE_CALLBACK";
        }
    }
}
