namespace BRaVe_Portal.Models.Enums
{
    public static class JobStatus
    {
        public const int Queuing = 1;
        public const int Running = 2;
        public const int Completed = 3;
        public const int CompletedDisabled = 4;
        public const int Failed = 5;
    }
}
