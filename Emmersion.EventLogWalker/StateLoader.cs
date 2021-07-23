using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Emmersion.EventLogWalker
{
    public interface IStateLoader
    {
        Task<WalkState> LoadInitialStateAsync(DateTimeOffset startInclusive, DateTimeOffset endExclusive,
            string resumeTokenJson);

        Task<WalkState> LoadNextStateAsync(WalkState previousState);
    }

    public class StateLoader : IStateLoader
    {
        private readonly IInsightsSystemApi insightsSystemApi;
        private readonly IJsonSerializer jsonSerializer;

        public StateLoader(IInsightsSystemApi insightsSystemApi, IJsonSerializer jsonSerializer)
        {
            this.insightsSystemApi = insightsSystemApi;
            this.jsonSerializer = jsonSerializer;
        }

        public async Task<WalkState> LoadInitialStateAsync(DateTimeOffset startInclusive, DateTimeOffset endExclusive,
            string resumeTokenJson)
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

                var initialState = await LoadStateFromPageAsync(new WalkState {Cursor = cursor});

                return new WalkState
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
                return new WalkState
                {
                    Cursor = cursor,
                    PreviousCursor = resumeToken?.Cursor,
                    Events = new List<InsightEvent>(),
                    PageEventIndex = resumeToken?.PageEventIndex ?? 0,
                    PageNumber = resumeToken?.PageNumber ?? 1,
                    TotalEventsProcessed = resumeToken?.TotalProcessedEvents ?? 0,
                    Exception = exception
                };
            }
        }

        public async Task<WalkState> LoadNextStateAsync(WalkState previousState)
        {
            try
            {
                if (previousState.Cursor == null)
                {
                    return new WalkState
                    {
                        Cursor = null,
                        PreviousCursor = previousState.PreviousCursor,
                        Events = new List<InsightEvent>(),
                        PageEventIndex = 0,
                        PageNumber = previousState.PageNumber + 1,
                        TotalEventsProcessed = previousState.TotalEventsProcessed
                    };
                }

                return await LoadStateFromPageAsync(previousState);
            }
            catch (Exception exception)
            {
                return new WalkState
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

        private async Task<WalkState> LoadStateFromPageAsync(WalkState previousState)
        {
            var page = await insightsSystemApi.GetPageAsync(previousState.Cursor);

            return new WalkState
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
