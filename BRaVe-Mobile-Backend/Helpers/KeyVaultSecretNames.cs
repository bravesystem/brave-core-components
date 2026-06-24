namespace BRaVe_Mobile_Backend.Helpers
{
    public static class KeyVaultSecretNames
    {
        public static class Sql
        {
            public const string PrimaryConnection = "AZURE_SQL_CONNECTIONSTRING";
        }

        public static class Logging
        {
            public const string App_Insight = "APPINSIGHTS_INSTRUMENTATIONKEY";
        }

        public static class Jwt
        {
            public const string Issuer = "JWT_ISSUER";
            public const string Enroll_Audience = "JWT_AUDIENCES";
            public const string Refresh_Audience = "JWT_REFRESH_AUDIENCE";
            public const string API_BASE_URL = "JWT_API_BASE_URL";
            public const string Key = "JWT_KEY";
        }
        public static class Storage
        {
            public const string RedisConnection = "CACHE_REDIS_CONNECTIONSTRING"; //cache
            public const string BlobConnection = "STORAGE_BLOB_CONNECTIONSTRING";
            public const string FileConnection = "STORAGE_FILE_CONNECTIONSTRING";
            public const string QueueConnection = "STORAGE_QUEUE_CONNECTIONSTRING";
        }

        public static class SecureStore
        {
            public const string Key_Vault = "KEY_VAULT_URL";
            public const string Pub_KeyName = "PUB_RSA_KEY_NAME";
        }

        public static class SBQ
        {
            public const string ServiceBusConnection = "AZURE_SB_CONNECTIONSTRING";
            public const string ServiceBusQueueName = "AZURE_SB_QUEUE_NAME";
        }
    }
}
