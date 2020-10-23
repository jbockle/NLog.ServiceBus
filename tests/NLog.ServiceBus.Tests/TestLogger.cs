using System;

namespace NLog.ServiceBus.Tests
{
    public class TestLogger
    {
        private readonly Logger _logger;
        private readonly LogFactory _factory;

        public TestLogger(Logger logger, LogFactory factory)
        {
            _logger = logger;
            _factory = factory;
        }

        public void Info(string message)
        {
            _logger.Info(message);
        }

        public void Error(Exception exception, string message)
        {
            _logger.Error(exception, message);
        }

        public void Flush()
        {
            _factory.Flush();
        }
    }
}