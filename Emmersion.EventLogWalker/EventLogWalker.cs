using System;
using System.Linq;
using System.Threading.Tasks;

namespace Emmersion.EventLogWalker
{
    public interface IEventLogWalker
    {
        Task<IEventLogWalkerStatus> WalkAsync(WalkArgs args, Func<InsightEvent, IEventLogWalkerStatus, Task> eventProcessor);
        Task<IEventLogWalkerStatus> WalkAsync(WalkArgs args, Action<InsightEvent, IEventLogWalkerStatus> eventProcessor);
    }

    public class EventLogWalker : IEventLogWalker
    {
        private readonly IStateLoader stateLoader;
        private readonly IStateProcessor stateProcessor;
        private readonly IJsonSerializer jsonSerializer;
        private readonly IResourceThrottle resourceThrottle;

        public EventLogWalker(IStateLoader stateLoader, IStateProcessor stateProcessor, IJsonSerializer jsonSerializer, IResourceThrottle resourceThrottle)
        {
            this.stateLoader = stateLoader;
            this.stateProcessor = stateProcessor;
            this.jsonSerializer = jsonSerializer;
            this.resourceThrottle = resourceThrottle;
            resourceThrottle.MinimumDurationBetweenAccess = TimeSpan.FromSeconds(1);
        }

        public Task<IEventLogWalkerStatus> WalkAsync(WalkArgs args, Func<InsightEvent, IEventLogWalkerStatus, Task> eventProcessor)
        {
            return WalkAsync(args, new EventProcessor(eventProcessor));
        }

        public Task<IEventLogWalkerStatus> WalkAsync(WalkArgs args, Action<InsightEvent, IEventLogWalkerStatus> eventProcessor)
        {
            return WalkAsync(args, new EventProcessor(eventProcessor));
        }

        private async Task<IEventLogWalkerStatus> WalkAsync(WalkArgs args, IEventProcessor eventProcessor)
        {
            var state = await stateLoader.LoadInitialStateAsync(args.StartInclusive, args.EndExclusive, args.ResumeToken);

            while (state.Events.Any() && state.Exception == null)
            {
                resourceThrottle.LastAccess = DateTimeOffset.UtcNow;
                state = await stateProcessor.ProcessStateAsync(eventProcessor, state);

                if (state.Exception != null)
                {
                    break;
                }
                resourceThrottle.WaitForNextAccess();

                state = await stateLoader.LoadNextStateAsync(state);
            }

            return new EventLogWalkerStatus(state, jsonSerializer);
        }
    }

    public class WalkArgs
    {
        public DateTimeOffset StartInclusive { get; set; } = DateTimeOffset.MinValue;
        public DateTimeOffset EndExclusive { get; set; } = DateTimeOffset.MaxValue;
        public string ResumeToken { get; set; }
    }
}
