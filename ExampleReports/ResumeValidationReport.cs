using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Emmersion.EventLogWalker;
using ExampleReports.Configuration;

namespace ExampleReports
{
    public interface IResumeValidationReport
    {
        Task GenerateAsync();
    }

    public class ResumeValidationReport : IResumeValidationReport
    {
        public const string StateFilePath =
            "c:\\tmp\\Emmersion.EventLogWalker\\" + nameof(ResumeValidationReport) + ".state.json";

        private readonly IEventLogWalker walker;
        private readonly IJsonSerializer jsonSerializer;
        private readonly IFileSystem fileSystem;
        private readonly IPager<InsightEvent> pager;
        private readonly TimeTracker eventTimeTracker;

        public ResumeValidationReport(IEventLogWalker walker, IJsonSerializer jsonSerializer, IFileSystem fileSystem, IPager<InsightEvent> pager)
        {
            this.walker = walker;
            this.jsonSerializer = jsonSerializer;
            this.fileSystem = fileSystem;
            this.pager = pager;

            eventTimeTracker = new TimeTracker(1);
        }

        public async Task GenerateAsync()
        {
            Console.WriteLine("WARNING: This report runs across the entire event log. This report is for example/test only. Recommend abort after a few events are processed.");

            var state = LoadState();
            var eventIds = state.EventIds;

            var finalStatus = await walker.WalkAsync(pager, new WalkArgs {ResumeToken = state.WalkerResumeToken},
                (insightEvent, status) =>
                {
                    if (status.PageStatus == PageStatus.Start)
                    {
                        PersistState(new ResumeValidationReportState(
                            walkerResumeToken: status.GetResumeToken(),
                            eventIds: eventIds));

                        eventTimeTracker.ItemCompleted($"Page number: {status.PageNumber}. TotalProcessedEvents: {status.TotalEventsProcessed}. ");
                    }


                    if (eventIds.Contains(insightEvent.Id))
                    {
                        throw new Exception($"The id {insightEvent.Id} was processed twice.");
                    }

                    eventIds.Add(insightEvent.Id);
                });

            if (finalStatus.Exception != null)
            {
                Console.WriteLine(finalStatus.Exception);
                return;
            }

            fileSystem.DeleteFile(StateFilePath);
        }

        private void PersistState(ResumeValidationReportState state)
        {
            var stateJson = jsonSerializer.Serialize(state);
            fileSystem.WriteFile(StateFilePath, stateJson);
        }

        private ResumeValidationReportState LoadState()
        {
            var stateJson = fileSystem.ReadFile(StateFilePath);
            return string.IsNullOrEmpty(stateJson)
                ? new ResumeValidationReportState(null, new HashSet<int>())
                : jsonSerializer.Deserialize<ResumeValidationReportState>(stateJson);
        }
    }

    public class ResumeValidationReportState
    {
        public ResumeValidationReportState(string walkerResumeToken, HashSet<int> eventIds)
        {
            WalkerResumeToken = walkerResumeToken;
            EventIds = eventIds;
        }

        public string WalkerResumeToken { get; }
        public HashSet<int> EventIds { get; }
    }
}
