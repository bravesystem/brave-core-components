using BRaVe_Biometric_Matching_Webjob.Helpers;
using BRaVe_Biometric_Matching_Webjob.Interfaces;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BRaVe_Biometric_Matching_Webjob.Services
{

    public class AzureServiceBusQueue : IJobQueue
    {
        private readonly QueueClient _client;
        private BrokeredMessage _currentMessage;

        public AzureServiceBusQueue(ISecretProvider secrerProvider)
        {
            string connectionStringName = KeyVaultSecretNames.SBQ.ServiceBusConnection;
            string queueNameAppSetting = KeyVaultSecretNames.SBQ.ServiceBusQueueName;

            var conn = secrerProvider.GetSecret(connectionStringName);
                //Environment.GetEnvironmentVariable(connectionStringName);
            var queueName = Environment.GetEnvironmentVariable(queueNameAppSetting);

            if (string.IsNullOrWhiteSpace(conn)) throw new InvalidOperationException($"Missing connection string '{connectionStringName}'.");
            if (string.IsNullOrWhiteSpace(queueName)) throw new InvalidOperationException($"Missing appSetting '{queueNameAppSetting}'.");

            _client = QueueClient.CreateFromConnectionString(conn, queueName);
        }

        public async Task<string> TryGetNextJobIdAsync(CancellationToken ct)
        {
            // PeekLock mode is default here
            _currentMessage = await _client.ReceiveAsync(TimeSpan.FromSeconds(2));
            if (_currentMessage == null) return null;

            // Expect jobId in message body (UTF-8 string)
            string jobId;
            using (var bodyStream = _currentMessage.GetBody<System.IO.Stream>())
            {
                if (bodyStream == null) return null;
                using (var ms = new System.IO.MemoryStream())
                {
                    await bodyStream.CopyToAsync(ms);
                    jobId = Encoding.UTF8.GetString(ms.ToArray());
                }
            }

            if (string.IsNullOrWhiteSpace(jobId))
            {
                // Alternatively, you can use properties: _currentMessage.Properties["jobId"]
                jobId = _currentMessage.Properties.ContainsKey("jobId") ? Convert.ToString(_currentMessage.Properties["jobId"]) : null;
            }

            return jobId;
        }

        public async Task CompleteAsync(CancellationToken ct)
        {
            if (_currentMessage != null)
            {
                // Option A (recommended): complete via message instance
                await _currentMessage.CompleteAsync();

                // Option B (alternative): complete via QueueClient using LockToken
                // await _client.CompleteAsync(_currentMessage.LockToken);

                _currentMessage = null;
            }
        }

        public async Task AbandonAsync(CancellationToken ct)
        {
            if (_currentMessage != null)
            {
                // Option A (recommended): abandon via message instance
                await _currentMessage.AbandonAsync();

                // Option B (alternative):
                // await _client.AbandonAsync(_currentMessage.LockToken);

                _currentMessage = null;
            }

            await Task.Yield();
        }
    }

}
