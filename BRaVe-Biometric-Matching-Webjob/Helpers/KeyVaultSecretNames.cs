namespace BRaVe_Biometric_Matching_Webjob.Helpers
{
    public static class KeyVaultSecretNames
    {
        public static class Sql
        {
            public const string PrimaryConnection = "AZURE_SQL_CONNECTIONSTRING";
        }
        public static class SBQ
        {
            public const string ServiceBusConnection = "AZURE_SB_CONNECTIONSTRING";
            public const string ServiceBusQueueName = "AZURE_SB_QUEUE_NAME";
        }

        public  static class Node
        {
            public const string ServerHost = "MATCHING_SERVER_HOSTNAME";
            public const string ServerParams = "MATCHING_SERVER_PARAMS";
        }
    }
}
