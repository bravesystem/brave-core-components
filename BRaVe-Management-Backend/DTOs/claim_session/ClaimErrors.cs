namespace BRaVe_Management_Backend.DTOs
{
    public static class ClaimErrors
    {
        public class NotFound : Exception { }
        public class CapacityExceeded : Exception { }
        public class DuplicateDevice : Exception { }
        public class AttestationFailed : Exception { }
        public class RateLimited : Exception { }
    }
}
