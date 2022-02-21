using System;
using System.Threading.Tasks;

namespace Emmersion.EventLogWalker
{
    internal interface IStateProcessor
    {
        Task<WalkState<TEvent>> ProcessStateAsync<TEvent>(IEventProcessor<TEvent> eventProcessor, WalkState<TEvent> state)
            where TEvent : class;
    }

    internal class StateProcessor : IStateProcessor
    {
        private readonly IJsonSerializer jsonSerializer;

        public StateProcessor(IJsonSerializer jsonSerializer)
        {
            this.jsonSerializer = jsonSerializer;
        }

        public async Task<WalkState<TEvent>> ProcessStateAsync<TEvent>(IEventProcessor<TEvent> eventProcessor, WalkState<TEvent> state)
            where TEvent : class
        {
            var processedEvents = 0;
            var walkStateInProgress = state;
            for (int i = state.PageEventIndex; i < state.Events.Count; i++)
            {
                try
                {
                    walkStateInProgress = new WalkState<TEvent>
                    {
                        Events = state.Events,
                        Cursor = state.Cursor,
                        PreviousCursor = state.PreviousCursor,
                        PageNumber = state.PageNumber,
                        PageEventIndex = i,
                        TotalEventsProcessed = state.TotalEventsProcessed + processedEvents
                    };

                    await eventProcessor.ProcessEventAsync(state.Events[i],
                        new EventLogWalkerStatus<TEvent>(walkStateInProgress, jsonSerializer));
                    processedEvents++;
                }
                catch (Exception exception)
                {
                    return new WalkState<TEvent>
                    {
                        Events = walkStateInProgress.Events,
                        Cursor = walkStateInProgress.Cursor,
                        PreviousCursor = walkStateInProgress.PreviousCursor,
                        PageNumber = walkStateInProgress.PageNumber,
                        PageEventIndex = walkStateInProgress.PageEventIndex,
                        TotalEventsProcessed = walkStateInProgress.TotalEventsProcessed,
                        Exception = exception
                    };
                }
            }

            return new WalkState<TEvent>
            {
                Events = state.Events,
                Cursor = state.Cursor,
                PreviousCursor = state.PreviousCursor,
                PageNumber = state.PageNumber,
                PageEventIndex = state.Events.Count,
                TotalEventsProcessed = state.TotalEventsProcessed + processedEvents
            };
        }
    }
}
