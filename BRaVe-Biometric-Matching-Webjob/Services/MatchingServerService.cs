using BRaVe_Biometric_Matching_Webjob.Exceptions;
using BRaVe_Biometric_Matching_Webjob.Helpers;
using BRaVe_Biometric_Matching_Webjob.Interfaces;
using BRaVe_Biometric_Matching_Webjob.Models;
using Neurotec.Biometrics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BRaVe_Biometric_Matching_Webjob.Services
{

    public class MatchingServerService : IMatchingServerService, INetworkTester
    {
        private MatchingServer matching;

        public MatchingServerService()
        {            
            /*if (string.IsNullOrWhiteSpace(_host))
                throw new InvalidOperationException("AppSetting 'ServerHost' is required.");*/

            matching = MatchingServer.Instance;

        }

        public async Task<bool> IsHealthyAsync(CancellationToken ct)
        {
            /*if (!await PingAsync(matching.getHost()))
                throw new MatchingErrors.PingError(matching.getHost());*/

            if (!await TestPortAsync(matching.getHost(), matching.getClientPort()))
                throw new MatchingErrors.TcpConnectionError(matching.getHost(), matching.getClientPort());

            return true;
        }

        public async Task PerformTask(ServerTask serverTask)
        {
            matching.performTask(serverTask);
        }

        public async Task ClearTask(ServerTask serverTask)
        {
            matching.clearTask(serverTask);
        }

        public async Task<bool> PingAsync(string host)
        {
            try
            {
                Ping myPing = new Ping();
                byte[] buffer = new byte[32];
                int timeout = 1500;
                PingOptions pingOptions = new PingOptions();
                PingReply reply = myPing.Send(host, timeout, buffer, pingOptions);
                if (reply.Status.ToString() == "Success") 
                    return reply.Status == IPStatus.Success;
                else 
                    return false;
            }
            catch 
            { 
                return false; 
            }
        }

        public async Task<bool> TestPortAsync(string host, int port)
        {
            using (TcpClient tcpClient = new TcpClient())
            {
                try
                {
                    tcpClient.Connect(host, port);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public async Task<NBiometricTask.SubjectCollection> GetSubjects(ServerTask serverTask)
        {
            return matching.getSubjects(serverTask);
        }

        public async Task<NSubject> CreateSubject(Biometric biometric)
        {
            return matching.CreateSubject(biometric);
        }

        public async Task AddSubject(ServerTask serverTask, NSubject subject)
        {
           matching.addSubject(serverTask, subject);    
        }

        public Exception getTaskError(ServerTask serverTask)
        {
            return matching.getTaskError(serverTask);
        }

        public int GetMinScore()
        {
            return matching.getMinScore();
        }

        public async Task clearTask(ServerTask serverTask)
        {
            matching.clearTask(serverTask);
        }

        public async Task<NBiometricStatus> GetStatus(ServerTask serverTask)
        {
            return matching.getStatus(serverTask);
        }
    }
}
