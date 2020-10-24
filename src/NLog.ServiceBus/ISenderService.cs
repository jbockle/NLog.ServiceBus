using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace NLog.ServiceBus
{
    public interface ISenderService
    {
        void Connect(string connectionString, string entityPath);

        Task SendMessagesAsync(IList<Message> messages);
    }
}
