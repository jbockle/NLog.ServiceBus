using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using NLog.Targets;

namespace NLog.ServiceBus
{
    /// <summary>
    /// NLog target to send log messages to an Azure Service Bus Queue
    /// </summary>
    [Target("ServiceBusQueue")]
    public class ServiceBusQueueTarget : ServiceBusTargetBase
    {
        public ServiceBusQueueTarget()
            : this(new QueueSenderService())
        {
        }

        internal ServiceBusQueueTarget(ISenderService sender)
            : base(sender)
        {
        }
    }

    internal class QueueSenderService : ISenderService
    {
        public IQueueClient Client { get; internal set; }

        public void Connect(string connectionString, string entityPath)
        {
            Client = new QueueClient(connectionString, entityPath);
        }

        public Task SendMessagesAsync(IList<Message> messages)
        {
            return Client.SendAsync(messages);
        }
    }
}
