using EL.Observability;

namespace Emmersion.EventLogWalker.Configuration
{
    public class LoggerSettings : ILoggerSettings
    {
        public bool ApplicationInsightsEnabled { get; } = false;
        public string FileLoggingPath { get; set; }
    }
}
