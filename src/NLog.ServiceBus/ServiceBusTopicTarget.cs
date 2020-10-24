using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using NLog.Targets;

namespace NLog.ServiceBus
{
    /// <summary>
    /// NLog target to send log messages to an Azure Service Bus Topic
    /// </summary>
    [Target("ServiceBusTopic")]
    public class ServiceBusTopicTarget : ServiceBusTargetBase
    {
        public ServiceBusTopicTarget()
            : this(new TopicSenderService())
        {
        }

        internal ServiceBusTopicTarget(ISenderService sender)
            : base(sender)
        {
        }
    }

    internal class TopicSenderService : ISenderService
    {
        public ITopicClient Client { get; internal set; }

        public void Connect(string connectionString, string entityPath)
        {
            Client = new TopicClient(connectionString, entityPath);
        }

        public Task SendMessagesAsync(IList<Message> messages)
        {
            return Client.SendAsync(messages);
        }
    }
}