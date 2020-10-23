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
    [Collection("queues")]
    public class ServiceBusQueueTargetTests : ServiceBusTargetBaseTests<ServiceBusQueueTarget>
    {
        public override ServiceBusQueueTarget CreateSut()
        {
            return new ServiceBusQueueTarget(MockSenderService.Object);
        }

        [Fact]
        public async Task DoesNotImplementSingleEvent()
        {
            var sut = new TestServiceBusQueueTarget();

            await Assert.ThrowsAsync<NotSupportedException>(sut.TryWriteAsyncTask);
        }

        [Fact]
        public void Connects()
        {
            var sut = new QueueSenderService();

            sut.Connect("Endpoint=sb://foo.servicebus;SharedAccessKeyName=baz;SharedAccessKey=Zm9vUGFzc3dvcmQ=", "bar");

            Assert.NotNull(sut.Client);
        }

        [Fact]
        public void SendsMessage()
        {
            var expected = new Message { MessageId = "foo" };
            var messages = new[] { expected, };

            var mockQueueClient = new Mock<IQueueClient>();

            var sut = new QueueSenderService { Client = mockQueueClient.Object };

            sut.SendMessagesAsync(messages);

            mockQueueClient.Verify(t => t.SendAsync(It.Is<IList<Message>>(l => l.First() == expected)));
        }
    }

    public class TestServiceBusQueueTarget : ServiceBusQueueTarget
    {
        public Task TryWriteAsyncTask()
        {
            return WriteAsyncTask(LogEventInfo.CreateNullEvent(), CancellationToken.None);
        }
    }
}