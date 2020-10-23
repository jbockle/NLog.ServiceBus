using System;
using Microsoft.Azure.ServiceBus;
using Moq;
using Xunit;

namespace NLog.ServiceBus.Tests
{
    public class MessageReflectionHelperTests
    {
        public MessageReflectionHelperTests()
        {
            MockMessage = new Mock<Message> { CallBase = true };
            Sut = new MessageReflectionHelper();
        }

        internal Mock<Message> MockMessage { get; }

        internal MessageReflectionHelper Sut { get; }

        [Fact]
        public void SetsString()
        {
            Sut.TrySetProperty("MessageId", "foo", MockMessage.Object);

            Assert.Equal("foo", MockMessage.Object.MessageId);
        }

        [Fact]
        public void SetsTimeSpan()
        {
            Sut.TrySetProperty("TimeToLive", "00:05:00", MockMessage.Object);

            Assert.Equal(TimeSpan.FromMinutes(5), MockMessage.Object.TimeToLive);
        }

        [Fact]
        public void WhenNull_DoesNotSet()
        {
            Assert.False(Sut.TrySetProperty("TimeToLive", null, MockMessage.Object));
        }

        [Fact]
        public void WhenInvalidKey_DoesNotSet()
        {
            Assert.False(Sut.TrySetProperty("foo", "bar", MockMessage.Object));
        }

        [Fact]
        public void WhenConversionFails_DoesNotSet()
        {
            Assert.False(Sut.TrySetProperty("TimeToLive", "invalid:timespan", MockMessage.Object));
        }
    }
}
