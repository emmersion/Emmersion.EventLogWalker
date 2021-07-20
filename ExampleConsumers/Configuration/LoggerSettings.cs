using EL.Observability;

namespace ExampleConsumers.Configuration
{
    public class LoggerSettings : ILoggerSettings
    {
        public bool ApplicationInsightsEnabled { get; } = false;
        public string FileLoggingPath { get; set; }
    }
}
