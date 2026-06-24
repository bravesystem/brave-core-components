using Serilog;

namespace BRaVe_Management_Backend.Exceptions
{
    public sealed class SqlUniqueConstraintViolationException : Exception
    {
        public SqlUniqueConstraintViolationException(Exception inner)
            : base("UNIQUE constraint violation.", inner)
        {
            // Log the exception when it's instantiated
            Log.Error(inner, "SQL UNIQUE constraint violation occurred.");
        }
    }
}
