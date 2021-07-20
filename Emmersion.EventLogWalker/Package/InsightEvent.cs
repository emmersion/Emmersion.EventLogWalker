using System;

namespace Emmersion.EventLogWalker.Package
{
    public class InsightEvent
    {
        public int Id { get; set; }
        public DateTimeOffset BrowserTimestamp { get; set; }
        public DateTimeOffset ServerTimestamp { get; set; }
        public Guid UserId { get; set; }
        public Guid AccountId { get; set; }
        public string AuthSession { get; set; }
        public string EventType { get; set; }
        public string Data { get; set; }
    }
}
