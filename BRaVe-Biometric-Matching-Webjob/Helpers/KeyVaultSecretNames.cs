namespace BRaVe_Biometric_Matching_Webjob.Helpers
{
    public static class KeyVaultSecretNames
    {
        public static class Sql
        {
            public const string PrimaryConnection = "AZURE-SQL-CONNECTIONSTRING";
        }
        public static class SBQ
        {
            public const string ServiceBusConnection = "AZURE-SB-CONNECTIONSTRING";
            public const string ServiceBusQueueName = "AZURE_SB_QUEUE_NAME";
        }

        public  static class Node
        {
            public const string ServerHost = "MATCHING_SERVER_HOSTNAME";
            public const string ServerParams = "MATCHING_SERVER_PARAMS";
        }

        public static class SecureStore
        {
            public const string Key_Vault = "KEY_VAULT_URL";
            public const string Pub_KeyName = "KEY_VAULT_KEY_NAME";
        }
    }
}
