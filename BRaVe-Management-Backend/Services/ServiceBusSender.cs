using Azure.Messaging.ServiceBus;
using BRaVe_Management_Backend.Interfaces;

namespace BRaVe_Management_Backend.Services
{
    public class ServiceBusSender : IServiceBusSender
    {
        private readonly ServiceBusClient _client;
        private readonly string _queueName;

        public ServiceBusSender(string connectionString, string queueName)
        {
            _client = new ServiceBusClient(connectionString);

            _queueName = queueName;
        }

        public async Task SendMessageAsync(string jobId)
        {
            var sender = _client.CreateSender(_queueName);
            var message = new ServiceBusMessage(jobId);
            await sender.SendMessageAsync(message);
        }
    }
}
