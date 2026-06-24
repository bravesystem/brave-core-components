using Azure.Messaging.ServiceBus;
using BRaVe_Mobile_Backend.Helpers;
using BRaVe_Mobile_Backend.Interfaces;

namespace BRaVe_Mobile_Backend.Services
{
    public class ServiceBusSender: IServiceBusSender
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
