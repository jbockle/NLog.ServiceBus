using NLog.Config;
using NLog.Layouts;

namespace NLog.ServiceBus
{
    /// <summary>
    /// A wrapper layout for <see cref="ServiceBusTargetBase.MessageProperties"/> and <see cref="ServiceBusTargetBase.UserProperties"/>
    /// </summary>
    [NLogConfigurationItem]
    public class ServiceBusProperty
    {
        /// <summary>
        /// Gets or sets the properties name
        /// </summary>
        [RequiredParameter]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the properties layout
        /// </summary>
        [RequiredParameter]
        public Layout Layout { get; set; }
    }
}