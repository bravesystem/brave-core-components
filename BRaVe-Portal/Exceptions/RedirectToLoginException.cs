using System.Net;

namespace BRaVe_Portal.Exceptions
{

    public sealed class RedirectToLoginException : Exception
    {
        public RedirectToLoginException(string? message = null, Exception? inner = null)
            : base(message ?? "User interaction required; redirect to /Login.", inner) { }
    }


}