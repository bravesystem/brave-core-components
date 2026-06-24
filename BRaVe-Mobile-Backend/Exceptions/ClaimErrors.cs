namespace BRaVe_Mobile_Backend.Exceptions
{
    public static class ClaimErrors
    {
        public class NotFound : Exception { }
        public class CapacityExceeded : Exception { }
        public class TenantMismatch : Exception { }
        public class DuplicateDevice : Exception { }
        public class AttestationFailed : Exception { }
        public class RateLimited : Exception { }
        public class BindingPoolExhausted : Exception { }

     
    }
}
