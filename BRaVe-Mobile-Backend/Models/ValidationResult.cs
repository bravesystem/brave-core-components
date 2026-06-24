namespace BRaVe_Mobile_Backend.Models
{
    public sealed class ValidationResult
    {
        public bool Ok { get; }
        public int StatusCode { get; }
        public string? Error { get; }

        private ValidationResult(bool ok, int statusCode, string? error)
        {
            Ok = ok; StatusCode = statusCode; Error = error;
        }

        public static ValidationResult Success() => new(true, StatusCodes.Status200OK, null);
        public static ValidationResult Unauthenticated(string e) => new(false, StatusCodes.Status401Unauthorized, e);
        public static ValidationResult Forbidden(string e) => new(false, StatusCodes.Status403Forbidden, e);
        public static ValidationResult NotFound(string e) => new(false, StatusCodes.Status404NotFound, e);
    }
}
