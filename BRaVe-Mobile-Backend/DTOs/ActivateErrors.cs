namespace BRaVe_Mobile_Backend.DTOs
{
    public static class ActivateErrors
    {
        public sealed class InvalidJws : Exception { }
        public sealed class JwsExpired : Exception { }
        public sealed class ClaimNotFound : Exception { }
        public sealed class ClaimConsumed : Exception { }
        public sealed class TenantMismatch : Exception { }
        public sealed class KeyThumbMismatch : Exception { }
        public sealed class Internal : Exception { public Internal(string m) : base(m) { } }
    }
}
