using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using NLog.Common;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;

namespace NLog.ServiceBus
{
    public abstract class ServiceBusTargetBase : AsyncTaskTarget
    {
        private readonly MessageReflectionHelper _reflectionHelper = new MessageReflectionHelper();

        protected ServiceBusTargetBase(ISenderService sender)
        {
            Sender = sender;
        }

        /// <summary>
        /// Gets the sender client
        /// </summary>
        public ISenderService Sender { get; internal set; }

        /// <summary>
        /// Gets or sets the service bus connection string
        /// </summary>
        [RequiredParameter]
        public Layout ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the service bus entity path
        /// </summary>
        [RequiredParameter]
        public Layout EntityPath { get; set; }

        /// <summary>
        /// Gets or sets the service bus message's content-type
        /// <para>
        /// <example>default: "application/octet-stream"</example>
        /// </para>
        /// </summary>
        public Layout ContentType { get; set; } = "application/octet-stream";

        /// <summary>
        /// Gets a list of user properties (aka custom properties) to add to the message
        /// <para>
        /// <example>
        /// &lt;target xsi:type="ServiceBusQueue"&gt;
        /// &lt;user-property name="foo" value="${layout}"/&gt;
        /// &lt;user-property name="bar" value="${layout}"/&gt;
        /// &lt;/target&gt;
        /// </example>
        /// </para>
        /// </summary>
        [ArrayParameter(typeof(ServiceBusProperty), "user-property")]
        public IList<ServiceBusProperty> UserProperties { get; } = new List<ServiceBusProperty>();

        /// <summary>
        /// Gets a list of message properties to add to the message
        /// <para>
        /// <example>
        /// &lt;target xsi:type="ServiceBusQueue"&gt;
        /// &lt;message-property name="Label" value="${logger}"/&gt;
        /// &lt;message-property name="ScheduledEnqueueTimeUtc" value="${date:format=o:universalTime=true}"/&gt;
        /// &lt;/target&gt;
        /// </example>
        /// </para>
        /// </summary>
        [ArrayParameter(typeof(ServiceBusProperty), "message-property")]
        public IList<ServiceBusProperty> MessageProperties { get; } = new List<ServiceBusProperty>();

        protected override void InitializeTarget()
        {
            base.InitializeTarget();

            var nullEvent = LogEventInfo.CreateNullEvent();
            var connectionString = RenderLogEvent(ConnectionString, nullEvent);
            var entityPath = RenderLogEvent(EntityPath, nullEvent);

            Sender.Connect(connectionString, entityPath);
        }

        protected override Task WriteAsyncTask(LogEventInfo logEvent, CancellationToken cancellationToken)
        {
            throw new NotSupportedException("should not get here, log event messages are batched in IList overload");
        }

        protected override async Task WriteAsyncTask(IList<LogEventInfo> logEvents, CancellationToken cancellationToken)
        {
            try
            {
                var messages = logEvents
                    .Select(CreateMessage)
                    .ToList();

                await Sender.SendMessagesAsync(messages);
            }
            catch (Exception exception)
            {
                InternalLogger.Error(exception, "{0}(Name={1}): Failed to send message", nameof(ServiceBusTopicTarget), Name);
            }
        }

        protected Message CreateMessage(LogEventInfo logEvent)
        {

            var body = GetMessageBody(logEvent);

            var message = new Message(body)
            {
                ContentType = RenderLogEvent(ContentType, logEvent),
            };

            LoadMessageProperties(logEvent, message);
            LoadUserProperties(logEvent, message);

            return message;
        }

        protected byte[] GetMessageBody(LogEventInfo logEvent)
        {
            var message = RenderLogEvent(Layout, logEvent);
            var body = Encoding.UTF8.GetBytes(message);

            return body;
        }

        private void LoadMessageProperties(LogEventInfo logEvent, Message message)
        {
            foreach (var property in MessageProperties)
            {
                var value = RenderLogEvent(property.Layout, logEvent);

                _reflectionHelper.TrySetProperty(property.Name, value, message);
            }
        }

        private void LoadUserProperties(LogEventInfo logEvent, Message message)
        {
            foreach (var property in UserProperties)
            {
                if (string.IsNullOrWhiteSpace(property.Name))
                {
                    InternalLogger.Warn("Cannot set user-property key that is null/whitespace");
                    continue;
                }

                var value = RenderLogEvent(property.Layout, logEvent);
                message.UserProperties[property.Name] = value;
            }
        }
    }
}