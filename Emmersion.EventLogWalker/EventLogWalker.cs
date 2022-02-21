﻿using System;
using System.Linq;
using System.Threading.Tasks;

namespace Emmersion.EventLogWalker
{
    public interface IEventLogWalker
    {
        Task<IEventLogWalkerStatus> WalkAsync<TEvent>(WalkArgs<TEvent> args, Func<TEvent, IEventLogWalkerStatus, Task> eventProcessor)
            where TEvent : class;
        Task<IEventLogWalkerStatus> WalkAsync<TEvent>(WalkArgs<TEvent> args, Action<TEvent, IEventLogWalkerStatus> eventProcessor)
            where TEvent : class;
    }

    internal class EventLogWalker : IEventLogWalker
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

        public Task<IEventLogWalkerStatus> WalkAsync<TEvent>(WalkArgs<TEvent> args, Func<TEvent, IEventLogWalkerStatus, Task> eventProcessor)
            where TEvent : class
        {
            return WalkAsync(args, new EventProcessor<TEvent>(eventProcessor));
        }

        public Task<IEventLogWalkerStatus> WalkAsync<TEvent>(WalkArgs<TEvent> args, Action<TEvent, IEventLogWalkerStatus> eventProcessor)
            where TEvent : class
        {
            return WalkAsync(args, new EventProcessor<TEvent>(eventProcessor));
        }

        private async Task<IEventLogWalkerStatus> WalkAsync<TEvent>(WalkArgs<TEvent> args, IEventProcessor<TEvent> eventProcessor)
            where TEvent : class
        {
            var state = await stateLoader.LoadInitialStateAsync(args.Pager, args.StartInclusive, args.EndExclusive, args.ResumeToken);

            while (state.Events.Any() && state.Exception == null)
            {
                resourceThrottle.LastAccess = DateTimeOffset.UtcNow;
                state = await stateProcessor.ProcessStateAsync(eventProcessor, state);

                if (state.Exception != null)
                {
                    break;
                }

                await resourceThrottle.WaitForNextAccessAsync();
                state = await stateLoader.LoadNextStateAsync(args.Pager, state);
            }

            return new EventLogWalkerStatus<TEvent>(state, jsonSerializer);
        }
    }

    public class WalkArgs<TEvent>
        where TEvent : class
    {
        public IPager<TEvent> Pager { get; set; }
        public DateTimeOffset StartInclusive { get; set; } = DateTimeOffset.MinValue;
        public DateTimeOffset EndExclusive { get; set; } = DateTimeOffset.MaxValue;
        public string ResumeToken { get; set; }
    }
}
