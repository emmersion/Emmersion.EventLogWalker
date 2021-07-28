using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Emmersion.EventLogWalker;
using ExampleReports.Configuration;

namespace ExampleReports
{
    public interface IAccountUserCountsReport
    {
        Task GenerateAsync(DateTimeOffset reportPeriodStartInclusive, DateTimeOffset reportPeriodEndExclusive);
    }

    public class AccountUserCountsReport : IAccountUserCountsReport
    {
        public const string StateFilePath = "c:\\tmp\\Emmersion.EventLogWalker\\" + nameof(AccountUserCountsReport) + ".state.json";

        private readonly IEventLogWalker eventLogWalker;
        private readonly ICsvWriter csvWriter;
        private readonly IJsonSerializer jsonSerializer;
        private readonly IFileSystem fileSystem;
        private readonly TimeTracker eventTimeTracker;

        public AccountUserCountsReport(IEventLogWalker eventLogWalker, ICsvWriter csvWriter,
            IJsonSerializer jsonSerializer, IFileSystem fileSystem)
        {
            this.eventLogWalker = eventLogWalker;
            this.csvWriter = csvWriter;
            this.jsonSerializer = jsonSerializer;
            this.fileSystem = fileSystem;

            eventTimeTracker = new TimeTracker(1);
        }

        public Dictionary<string, HashSet<Guid>> EventAccountCounts { get; set; } =
            new Dictionary<string, HashSet<Guid>>();

        public Dictionary<string, HashSet<Guid>> EventUserCounts { get; set; } =
            new Dictionary<string, HashSet<Guid>>();

        public async Task GenerateAsync(DateTimeOffset reportPeriodStartInclusive,
            DateTimeOffset reportPeriodEndExclusive)
        {
            Console.WriteLine($@"Starting report generation.
args =>
{{
   {nameof(reportPeriodStartInclusive)}: {reportPeriodStartInclusive:yyyy-MM-dd HH:mm:ss.fffffff +00:00}
   {nameof(reportPeriodEndExclusive)}:   {reportPeriodEndExclusive:yyyy-MM-dd HH:mm:ss.fffffff +00:00}
}}");

            var resumeToken = LoadState();

            //  NOTE: See ExampleReports.Configuration.DependencyInjectionConfig to understand the package dependency needs
            var status = await eventLogWalker.WalkAsync(
                new WalkArgs
                {
                    StartInclusive = reportPeriodStartInclusive,
                    EndExclusive = reportPeriodEndExclusive,
                    ResumeToken = resumeToken
                }, ProcessEvent);

            if (status.Exception != null)
            {
                Console.WriteLine($"Walker exited with error: {status.Exception}");
                PersistState(status.GetResumeToken());
                return;
            }

            Console.WriteLine($"Walker exited successfully after processing {status.TotalEventsProcessed} events");

            var eventCounts = MapEventCounts();

            var fileName =
                $"{nameof(AccountUserCountsReport)}_(from {reportPeriodStartInclusive:yyyy-MM-dd} to {reportPeriodEndExclusive:yyyy-MM-dd})_{DateTimeOffset.UtcNow:yyyy-MM-dd HH_mm_ss}.csv";
            var headerRow =
                $"{nameof(EventCounts.EventType)},{nameof(EventCounts.DistinctAccounts)},{nameof(EventCounts.DistinctUsers)}";
            csvWriter.WriteAll(fileName, headerRow, eventCounts);

            Console.WriteLine($"Wrote to: {fileName}");

            fileSystem.DeleteFile(StateFilePath);
        }

        private List<EventCounts> MapEventCounts()
        {
            var keys = EventAccountCounts.Keys;

            return keys.Select(x => new EventCounts
            {
                EventType = x,
                DistinctAccounts = EventAccountCounts[x].Count,
                DistinctUsers = EventUserCounts[x].Count
            }).ToList();
        }

        public void ProcessEvent(InsightEvent insightEvent, IEventLogWalkerStatus status)
        {
            if (status.PageStatus == PageStatus.Start)
            {
                PersistState(status.GetResumeToken());
                eventTimeTracker.ItemCompleted($"Page number: {status.PageNumber}. TotalProcessedEvents: {status.TotalEventsProcessed}. ");
            }

            StoreDistinctAccounts(insightEvent);
            StoreDistinctUsers(insightEvent);
        }

        private void StoreDistinctAccounts(InsightEvent insightEvent)
        {
            if (EventAccountCounts.ContainsKey(insightEvent.EventType))
            {
                EventAccountCounts[insightEvent.EventType].Add(insightEvent.AccountId);
            }
            else
            {
                EventAccountCounts[insightEvent.EventType] = new HashSet<Guid> {insightEvent.AccountId};
            }
        }

        private void StoreDistinctUsers(InsightEvent insightEvent)
        {
            if (EventUserCounts.ContainsKey(insightEvent.EventType))
            {
                EventUserCounts[insightEvent.EventType].Add(insightEvent.UserId);
            }
            else
            {
                EventUserCounts[insightEvent.EventType] = new HashSet<Guid> {insightEvent.UserId};
            }
        }

        private void PersistState(string walkerResumeToken)
        {
            var state = new AccountUserCountsReportState
            {
                WalkerResumeToken = walkerResumeToken,
                EventAccountCounts = EventAccountCounts,
                EventUserCounts = EventUserCounts
            };
            var stateJson = jsonSerializer.Serialize(state);
            fileSystem.WriteFile(StateFilePath, stateJson);
        }

        private string LoadState()
        {
            var stateJson = fileSystem.ReadFile(StateFilePath);
            if (string.IsNullOrEmpty(stateJson))
            {
                return null;
            }

            var state = jsonSerializer.Deserialize<AccountUserCountsReportState>(stateJson);
            EventAccountCounts = state.EventAccountCounts;
            EventUserCounts = state.EventUserCounts;
            return state.WalkerResumeToken;
        }
    }

    public class EventCounts
    {
        public string EventType { get; set; }
        public int DistinctAccounts { get; set; }
        public int DistinctUsers { get; set; }

        public override string ToString()
        {
            return $"{EventType},{DistinctAccounts},{DistinctUsers}";
        }
    }

    public class AccountUserCountsReportState
    {
        public string WalkerResumeToken { get; set; }
        public Dictionary<string, HashSet<Guid>> EventAccountCounts { get; set; }
        public Dictionary<string, HashSet<Guid>> EventUserCounts { get; set; }
    }
}
