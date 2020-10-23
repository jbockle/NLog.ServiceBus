using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Moq;
using Xunit;

namespace NLog.ServiceBus.Tests
{
    [Collection("topics")]
    public class ServiceBusTopicTargetTests : ServiceBusTargetBaseTests<ServiceBusTopicTarget>
    {
        public override ServiceBusTopicTarget CreateSut()
        {
            return new ServiceBusTopicTarget(MockSenderService.Object);
        }

        [Fact]
        public async Task DoesNotImplementSingleEvent()
        {
            var sut = new TestServiceBusTopicTarget();

            await Assert.ThrowsAsync<NotSupportedException>(sut.TryWriteAsyncTask);
        }

        [Fact]
        public void Connects()
        {
            var sut = new TopicSenderService();

            sut.Connect("Endpoint=sb://foo.servicebus;SharedAccessKeyName=baz;SharedAccessKey=Zm9vUGFzc3dvcmQ=", "bar");

            Assert.NotNull(sut.Client);
        }

        [Fact]
        public void SendsMessage()
        {
            var expected = new Message { MessageId = "foo" };
            var messages = new[] { expected, };

            var mockTopicClient = new Mock<ITopicClient>();

            var sut = new TopicSenderService { Client = mockTopicClient.Object };

            sut.SendMessagesAsync(messages);

            mockTopicClient.Verify(t => t.SendAsync(It.Is<IList<Message>>(l => l.First() == expected)));
        }
    }

    public class TestServiceBusTopicTarget : ServiceBusTopicTarget
    {
        public Task TryWriteAsyncTask()
        {
            return WriteAsyncTask(LogEventInfo.CreateNullEvent(), CancellationToken.None);
        }
    }
}
