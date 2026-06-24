using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BRaVe_Biometric_Matching_Webjob.Exceptions
{
    public static class MatchingErrors
    {
        public class StatusUnknown : Exception { }
        public class TaskUnknown : Exception { }
        public class PingError : Exception {

            public PingError(string host)
                    : base($"Ping to {host} failed: Request timed out.")
            {
            }

        }
        public class TcpConnectionError : Exception {
            
            public TcpConnectionError(string host, int port)
                    : base($"TCP connection to {host}:{port} failed.")
            {
            }

        }
    }
}
