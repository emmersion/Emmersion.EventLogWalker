using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Emmersion.EventLogWalker
{
    internal interface IStateLoader
    {
        Task<WalkState<TEvent>> LoadInitialStateAsync<TEvent>(IPager<TEvent> pager, DateTimeOffset startInclusive, DateTimeOffset endExclusive, string resumeTokenJson)
            where TEvent : class;
        Task<WalkState<TEvent>> LoadNextStateAsync<TEvent>(IPager<TEvent> pager, WalkState<TEvent> previousState)
            where TEvent : class;
    }

    internal class StateLoader : IStateLoader
    {
        private readonly IJsonSerializer jsonSerializer;

        public StateLoader(IJsonSerializer jsonSerializer)
        {
            this.jsonSerializer = jsonSerializer;
        }

        public async Task<WalkState<TEvent>> LoadInitialStateAsync<TEvent>(IPager<TEvent> pager,
            DateTimeOffset startInclusive, DateTimeOffset endExclusive, string resumeTokenJson)
            where TEvent : class
        {
            var cursor = new Cursor
            {
                StartInclusive = startInclusive,
                EndExclusive = endExclusive
            };

            ResumeToken resumeToken = null;
            try
            {
                if (!string.IsNullOrEmpty(resumeTokenJson))
                {
                    resumeToken = jsonSerializer.Deserialize<ResumeToken>(resumeTokenJson);
                    cursor = resumeToken.Cursor ?? cursor;
                }

                var initialState = await LoadStateFromPageAsync(pager, new WalkState<TEvent> {Cursor = cursor});

                return new WalkState<TEvent>
                {
                    Events = initialState.Events,
                    Cursor = initialState.Cursor,
                    PreviousCursor = cursor,
                    PageNumber = resumeToken?.PageNumber ?? initialState.PageNumber,
                    PageEventIndex = resumeToken?.PageEventIndex ?? initialState.PageEventIndex,
                    TotalEventsProcessed = resumeToken?.TotalProcessedEvents ?? initialState.TotalEventsProcessed
                };
            }
            catch (Exception exception)
            {
                return new WalkState<TEvent>
                {
                    Cursor = cursor,
                    PreviousCursor = resumeToken?.Cursor,
                    Events = new List<TEvent>(),
                    PageEventIndex = resumeToken?.PageEventIndex ?? 0,
                    PageNumber = resumeToken?.PageNumber ?? 1,
                    TotalEventsProcessed = resumeToken?.TotalProcessedEvents ?? 0,
                    Exception = exception
                };
            }
        }

        public async Task<WalkState<TEvent>> LoadNextStateAsync<TEvent>(IPager<TEvent> pager, WalkState<TEvent> previousState)
            where TEvent : class
        {
            try
            {
                if (previousState.Cursor == null)
                {
                    return new WalkState<TEvent>
                    {
                        Cursor = null,
                        PreviousCursor = previousState.PreviousCursor,
                        Events = new List<TEvent>(),
                        PageEventIndex = 0,
                        PageNumber = previousState.PageNumber + 1,
                        TotalEventsProcessed = previousState.TotalEventsProcessed
                    };
                }

                return await LoadStateFromPageAsync(pager, previousState);
            }
            catch (Exception exception)
            {
                return new WalkState<TEvent>
                {
                    PageEventIndex = previousState.PageEventIndex,
                    Cursor = previousState.Cursor,
                    PreviousCursor = previousState.PreviousCursor,
                    Events = previousState.Events,
                    PageNumber = previousState.PageNumber,
                    TotalEventsProcessed = previousState.TotalEventsProcessed,
                    Exception = exception
                };
            }
        }

        private async Task<WalkState<TEvent>> LoadStateFromPageAsync<TEvent>(IPager<TEvent> pager, WalkState<TEvent> previousState)
            where TEvent : class
        {
            var page = await pager.GetPageAsync(previousState.Cursor);

            return new WalkState<TEvent>
            {
                Cursor = page.NextPage,
                PreviousCursor = previousState.Cursor,
                Events = page.Events,
                PageEventIndex = 0,
                PageNumber = previousState.PageNumber + 1,
                TotalEventsProcessed = previousState.TotalEventsProcessed
            };
        }
    }
}
