using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Moq;
using Xunit;

namespace NLog.ServiceBus.Tests
{
    public abstract class ServiceBusTargetBaseTests<T>
        where T : ServiceBusTargetBase
    {
        private LogFactory logFactory = new LogFactory();

        protected ServiceBusTargetBaseTests()
        {
            MockSenderService = new Mock<ISenderService>();
        }

        public Mock<ISenderService> MockSenderService { get; }

        public abstract T CreateSut();

        [Fact]
        public void SendsLogEventAsMessage()
        {
            var expectedByteArray = Encoding.UTF8.GetBytes("Hello World");

            MockSenderService
                .Setup(s => s.SendMessagesAsync(
                    It.Is<IList<Message>>(messages => messages.Any(o => o.Body.SequenceEqual(expectedByteArray)))))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var logger = CreateTestLogger(configureTarget: target =>
            {
                target.Layout = "${message}";
            });

            logger.Info("Hello World");
            logger.Flush();

            MockSenderService.Verify();
        }

        [Fact]
        public void BatchesLogEventsAsMessages()
        {
            var logger = CreateTestLogger(configureTarget: target =>
            {
                target.Layout = "${message}";
                target.BatchSize = 5;
            });

            for (var i = 0; i < 10; i++)
            {
                logger.Info("msg 1");
            }

            logger.Flush();

            MockSenderService
                .Verify(s => s.SendMessagesAsync(It.Is<IList<Message>>(messages => messages.Count() == 5)),
                    Times.Exactly(2));
        }

        [Theory]
        [InlineData(6)]
        [InlineData(8)]
        [InlineData(101)]
        public void WhenUnEvenBatch_BatchesLogEventsAsMessages(int expectedMessageCount)
        {
            var actualCount = 0;

            MockSenderService
                .Setup(s => s.SendMessagesAsync(It.IsAny<IList<Message>>()))
                .Returns((IEnumerable<Message> messages) =>
                {
                    actualCount += messages.Count();
                    return Task.CompletedTask;
                });

            var logger = CreateTestLogger(configureTarget: target =>
            {
                target.Layout = "${message}";
                target.BatchSize = 5;
            });

            for (var i = 0; i < expectedMessageCount; i++)
            {
                logger.Info($"msg {i}");
            }

            logger.Flush();

            Assert.Equal(actualCount, expectedMessageCount);
        }

        [Fact]
        public void SetsContentType()
        {
            MockSenderService
                .Setup(s => s.SendMessagesAsync(
                    It.Is<IList<Message>>(messages => messages.All(o => o.ContentType == "Test"))))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var logger = CreateTestLogger(configureTarget: target =>
            {
                target.ContentType = "${logger}";
            });

            logger.Info("Hello World");
            logger.Flush();

            MockSenderService.Verify();
        }

        [Fact]
        public void AddsUserProperties()
        {
            MockSenderService
                .Setup(s => s.SendMessagesAsync(
                    It.Is<IList<Message>>(messages => messages.Any(o => o.UserProperties["level"].ToString() == "Info"))))
                .Returns(Task.CompletedTask)
                .Verifiable();
            MockSenderService
                .Setup(s => s.SendMessagesAsync(
                    It.Is<IList<Message>>(messages => messages.Any(o => o.UserProperties["level"].ToString() == "Error"))))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var logger = CreateTestLogger(configureTarget: target =>
            {
                target.UserProperties.Add(new ServiceBusProperty { Name = "level", Layout = "${level}" });
            });

            logger.Info("Hello World");
            logger.Error(new Exception("ya basic"), "foo");
            logger.Flush();

            MockSenderService.Verify();
        }

        [Fact]
        public void WhenUserPropertyNameIsEmpty_SkipsProperty()
        {
            MockSenderService
                .Setup(s => s.SendMessagesAsync(
                    It.Is<IList<Message>>(messages => messages.All(o => !o.UserProperties.Any()))))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var logger = CreateTestLogger(configureTarget: target =>
            {
                target.UserProperties.Add(new ServiceBusProperty { Name = "", Layout = "${level}" });
            });

            logger.Info("Hello World");
            logger.Flush();

            MockSenderService.Verify();
        }

        [Fact]
        public void SetsMessageProperties()
        {
            var expectedEnqueueTime = DateTime.Today.AddDays(1).ToUniversalTime();

            MockSenderService
                .Setup(s => s.SendMessagesAsync(It.Is<IList<Message>>(messages => messages.All(o =>
                    o.MessageId == "Info"
                    && o.TimeToLive == TimeSpan.Parse("00:05:00")
                    && o.ScheduledEnqueueTimeUtc == expectedEnqueueTime))))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var logger = CreateTestLogger(configureTarget: target =>
            {
                target.MessageProperties.Add(new ServiceBusProperty { Name = "MessageId", Layout = "${level}" });
                target.MessageProperties.Add(new ServiceBusProperty { Name = "TimeToLive", Layout = "00:05:00" });
                target.MessageProperties.Add(new ServiceBusProperty { Name = "ScheduledEnqueueTimeUtc", Layout = expectedEnqueueTime.ToString("o") });
            });

            logger.Info("Hello World");
            logger.Flush();

            MockSenderService.Verify();
        }

        [Fact]
        public void WhenExceptionOccurs_LogsToInternal()
        {
            MockSenderService
                .Setup(s => s.SendMessagesAsync(It.IsAny<IList<Message>>()))
                .ThrowsAsync(new Exception("foo-ex"))
                .Verifiable();

            var logger = CreateTestLogger();

            logger.Info("Hello World");
            logger.Flush();

            MockSenderService.Verify();
        }

        [Theory]
        [InlineData("", TransportType.Amqp)]
        [InlineData("Amqp", TransportType.Amqp)]
        [InlineData("amqp", TransportType.Amqp)]
        [InlineData("amqpwebsockets", TransportType.AmqpWebSockets)]
        [InlineData("AmqpWebSockets", TransportType.AmqpWebSockets)]
        [InlineData("AmqpWeb", TransportType.Amqp)]
        public void SetsTransportType(string transportTypeLayout, TransportType expectedTransportType)
        {
            var logger = CreateTestLogger(configureTarget: target => target.TransportType = transportTypeLayout);

            logger.Info("foo");

            var ttarget = (T)logFactory.Configuration.AllTargets.First(target => target is T);

            var connectionString = ttarget.GetConnectionString(LogEventInfo.CreateNullEvent());
            var actualTransportType = connectionString.Split(';')
                .Select(kvp => kvp.Split('='))
                .FirstOrDefault(kvp => kvp[0].Equals("TransportType"))?[1] ?? TransportType.Amqp.ToString();

            Assert.Equal(expectedTransportType.ToString(), actualTransportType);
        }

        private TestLogger CreateTestLogger(
            string loggerName = "Test",
            Action<T> configureTarget = null,
            Action<Config.LoggingConfiguration> configureNLog = null
        )
        {
            var logConfig = new Config.LoggingConfiguration(logFactory);

            logConfig.Variables["ConnectionString"] = "Endpoint=foo-connection-string.foo.com";
            logConfig.Variables["EntityPath"] = "baz-topic";

            configureNLog?.Invoke(logConfig);

            var sut = CreateSut();

            sut.ConnectionString = "${var:ConnectionString}";
            sut.EntityPath = "${var:EntityPath}";

            configureTarget?.Invoke(sut);

            logConfig.AddRuleForAllLevels(sut);
            logFactory.Configuration = logConfig;

            return new TestLogger(logFactory.GetLogger(loggerName), logFactory);
        }
    }
}