using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BRaVe_Biometric_Matching_Webjob.Interfaces
{
    public interface INetworkTester
    {

        /// <summary>
        /// Pings a host to check if it is reachable.
        /// </summary>
        /// <param name="host">Hostname or IP address.</param>
        /// <returns>True if ping succeeds, otherwise false.</returns>
        Task<bool> PingAsync(string host);

        /// <summary>
        /// Tests connectivity to a specific port on a host.
        /// </summary>
        /// <param name="host">Hostname or IP address.</param>
        /// <param name="port">Port number to test.</param>
        /// <returns>True if connection succeeds, otherwise false.</returns>
        Task<bool> TestPortAsync(string host, int port);

    }
}
