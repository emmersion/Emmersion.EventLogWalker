using System;

namespace Emmersion.EventLogWalker
{
    public interface IEventLogWalkerStatus
    {
        int TotalEventsProcessed { get; }
        int PageNumber { get; }
        int PageEventIndex { get; }
        int PageEventsCount { get; }
        Exception Exception { get; }
        PageStatus PageStatus { get; }
        string GetResumeToken();
    }

    internal class EventLogWalkerStatus : IEventLogWalkerStatus
    {
        private readonly WalkState state;
        private readonly IJsonSerializer jsonSerializer;

        public EventLogWalkerStatus(WalkState state, IJsonSerializer jsonSerializer)
        {
            this.state = state;
            this.jsonSerializer = jsonSerializer;
        }

        public int TotalEventsProcessed => state.TotalEventsProcessed;
        public int PageNumber => state.PageNumber;
        public int PageEventIndex => state.PageEventIndex;
        public int PageEventsCount => state.Events.Count;
        public Exception Exception => state.Exception;
        public PageStatus PageStatus => (PageEventIndex: state.PageEventIndex, PageEventsCount: state.Events.Count) switch
        {
            (PageEventIndex: 0, PageEventsCount: 0) => PageStatus.Empty,
            (PageEventIndex: 0, PageEventsCount: _) => PageStatus.Start,
            (PageEventIndex: var index, PageEventsCount: var eventsCount) when index == eventsCount - 1 => PageStatus.End,
            (PageEventIndex: var index, PageEventsCount: var eventsCount) when index == eventsCount => PageStatus.Done,
            (PageEventIndex: _, PageEventsCount: _) => PageStatus.InProgress
        };

        public string GetResumeToken()
        {
            var resumeToken = new ResumeToken
            {
                Cursor = state.PreviousCursor,
                PageEventIndex = state.PageEventIndex,
                PageNumber = state.PageNumber,
                TotalProcessedEvents = state.TotalEventsProcessed
            };
            return jsonSerializer.Serialize(resumeToken);
        }
    }

    public enum PageStatus
    {
        Empty,
        Start,
        InProgress,
        End,
        Done
    }
}
