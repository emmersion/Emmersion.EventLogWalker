using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Emmersion.EventLogWalker;
using Emmersion.Testing;
using ExampleReports.Configuration;
using Moq;
using NUnit.Framework;

namespace ExampleReports.UnitTests
{
    public class AccountUserCountsReportTests : With_an_automocked<AccountUserCountsReport>
    {
        [Test]
        public async Task When_generating()
        {
            var reportPeriodStartInclusive = DateTimeOffset.UtcNow.AddDays(-7);
            var reportPeriodEndExclusive = DateTimeOffset.UtcNow;

            var eventType1 = RandomString();
            ClassUnderTest.EventAccountCounts[eventType1] = new HashSet<Guid> {NewGuid()};
            ClassUnderTest.EventUserCounts[eventType1] = new HashSet<Guid> {NewGuid(), NewGuid()};

            var eventType2 = RandomString();
            ClassUnderTest.EventAccountCounts[eventType2] = new HashSet<Guid> {NewGuid(), NewGuid()};
            ClassUnderTest.EventUserCounts[eventType2] = new HashSet<Guid> {NewGuid(), NewGuid(), NewGuid()};

            var expectedEventCounts = new List<EventCounts>
            {
                new EventCounts
                {
                    EventType = eventType1,
                    DistinctAccounts = 1,
                    DistinctUsers = 2
                },
                new EventCounts
                {
                    EventType = eventType2,
                    DistinctAccounts = 2,
                    DistinctUsers = 3
                }
            };

            GetMock<IFileSystem>().Setup(x => x.ReadFile(AccountUserCountsReport.StateFilePath)).Returns((string)null);

            string capturedFileName = null;
            string capturedHeaderRow = null;
            List<EventCounts> capturedRecords = null;
            GetMock<ICsvWriter>().Setup(x => x.WriteAll(IsAny<string>(), IsAny<string>(), IsAny<List<EventCounts>>()))
                .Callback<string, string, List<EventCounts>>((fileName, headerRow, records) =>
                {
                    capturedFileName = fileName;
                    capturedHeaderRow = headerRow;
                    capturedRecords = records;
                });

            WalkArgs capturedWalkArgs = null;
            Action<WalkedEvent, IEventLogWalkerStatus> capturedFunc = null;
            GetMock<IEventLogWalker>().Setup(x => x.WalkAsync(IsAny<WalkArgs>(), IsAny<Action<WalkedEvent, IEventLogWalkerStatus>>()))
                .Callback<WalkArgs, Action<WalkedEvent, IEventLogWalkerStatus>>((args, func) =>
                {
                    capturedWalkArgs = args;
                    capturedFunc = func;
                })
                .ReturnsAsync(new TestEventLogWalkerStatus());

            await ClassUnderTest.GenerateAsync(reportPeriodStartInclusive, reportPeriodEndExclusive);

            GetMock<IFileSystem>().Verify(x => x.DeleteFile(AccountUserCountsReport.StateFilePath));

            Assert.That(capturedWalkArgs.StartInclusive, Is.EqualTo(reportPeriodStartInclusive));
            Assert.That(capturedWalkArgs.EndExclusive, Is.EqualTo(reportPeriodEndExclusive));
            Assert.That(capturedWalkArgs.ResumeToken, Is.Null);
            Assert.That(capturedFunc, Is.EqualTo((Action<WalkedEvent, IEventLogWalkerStatus>)ClassUnderTest.ProcessEvent));

            Assert.That(capturedFileName, Is.EqualTo($"{nameof(AccountUserCountsReport)}_(from {reportPeriodStartInclusive:yyyy-MM-dd} to {reportPeriodEndExclusive:yyyy-MM-dd})_{DateTimeOffset.UtcNow:yyyy-MM-dd HH_mm_ss}.csv"));
            Assert.That(capturedHeaderRow, Is.EqualTo($"{nameof(EventCounts.EventType)},{nameof(EventCounts.DistinctAccounts)},{nameof(EventCounts.DistinctUsers)}"));

            Assert.That(capturedRecords.First().EventType, Is.EqualTo(expectedEventCounts.First().EventType));
            Assert.That(capturedRecords.First().DistinctAccounts, Is.EqualTo(expectedEventCounts.First().DistinctAccounts));
            Assert.That(capturedRecords.First().DistinctUsers, Is.EqualTo(expectedEventCounts.First().DistinctUsers));

            Assert.That(capturedRecords.Last().EventType, Is.EqualTo(expectedEventCounts.Last().EventType));
            Assert.That(capturedRecords.Last().DistinctAccounts, Is.EqualTo(expectedEventCounts.Last().DistinctAccounts));
            Assert.That(capturedRecords.Last().DistinctUsers, Is.EqualTo(expectedEventCounts.Last().DistinctUsers));
        }

        [Test]
        public async Task When_generating_from_resume()
        {
            var reportPeriodStartInclusive = DateTimeOffset.UtcNow.AddDays(-7);
            var reportPeriodEndExclusive = DateTimeOffset.UtcNow;
            var reportState = new AccountUserCountsReportState
            {
                EventAccountCounts = new Dictionary<string, HashSet<Guid>>(),
                EventUserCounts = new Dictionary<string, HashSet<Guid>>(),
                WalkerResumeToken = RandomString()
            };
            var stateJson = RandomString();

            GetMock<IFileSystem>().Setup(x => x.ReadFile(AccountUserCountsReport.StateFilePath)).Returns(stateJson);
            GetMock<IJsonSerializer>().Setup(x => x.Deserialize<AccountUserCountsReportState>(stateJson)).Returns(reportState);

            WalkArgs capturedWalkArgs = null;
            GetMock<IEventLogWalker>().Setup(x => x.WalkAsync(IsAny<WalkArgs>(), IsAny<Action<WalkedEvent, IEventLogWalkerStatus>>()))
                .Callback<WalkArgs, Action<WalkedEvent, IEventLogWalkerStatus>>((args, _) => capturedWalkArgs = args)
                .ReturnsAsync(new TestEventLogWalkerStatus());

            await ClassUnderTest.GenerateAsync(reportPeriodStartInclusive, reportPeriodEndExclusive);

            Assert.That(ClassUnderTest.EventAccountCounts, Is.SameAs(reportState.EventAccountCounts));
            Assert.That(ClassUnderTest.EventUserCounts, Is.SameAs(reportState.EventUserCounts));
            Assert.That(capturedWalkArgs.StartInclusive, Is.EqualTo(reportPeriodStartInclusive));
            Assert.That(capturedWalkArgs.EndExclusive, Is.EqualTo(reportPeriodEndExclusive));
            Assert.That(capturedWalkArgs.ResumeToken, Is.EqualTo(reportState.WalkerResumeToken));
        }

        [Test]
        public async Task When_generating_and_an_error_occurs()
        {
            var reportPeriodStartInclusive = DateTimeOffset.UtcNow.AddDays(-7);
            var reportPeriodEndExclusive = DateTimeOffset.UtcNow;
            var exception = new Exception(RandomString());

            var resumeToken = RandomString();
            var stateJson = RandomString();

            var statusMock = GetMock<IEventLogWalkerStatus>();
            statusMock.SetupGet(x => x.Exception).Returns(exception);
            statusMock.Setup(x => x.GetResumeToken()).Returns(resumeToken);

            AccountUserCountsReportState capturedAccountUserCountsReportState = null;
            GetMock<IJsonSerializer>().Setup(x => x.Serialize(IsAny<AccountUserCountsReportState>()))
                .Callback<AccountUserCountsReportState>(reportState => capturedAccountUserCountsReportState = reportState)
                .Returns(stateJson);

            GetMock<IEventLogWalker>().Setup(x => x.WalkAsync(IsAny<WalkArgs>(), IsAny<Action<WalkedEvent, IEventLogWalkerStatus>>()))
                .ReturnsAsync(statusMock.Object);

            await ClassUnderTest.GenerateAsync(reportPeriodStartInclusive, reportPeriodEndExclusive);

            GetMock<ICsvWriter>().VerifyNever(x => x.WriteAll(IsAny<string>(), IsAny<string>(), IsAny<List<EventCounts>>()));
            GetMock<IFileSystem>().Verify(x => x.WriteFile(AccountUserCountsReport.StateFilePath, stateJson));

            Assert.That(capturedAccountUserCountsReportState.EventAccountCounts, Is.SameAs(ClassUnderTest.EventAccountCounts));
            Assert.That(capturedAccountUserCountsReportState.EventUserCounts, Is.SameAs(ClassUnderTest.EventUserCounts));
            Assert.That(capturedAccountUserCountsReportState.WalkerResumeToken, Is.EqualTo(resumeToken));
        }

        [Test]
        public void When_processing_multiple_events_of_the_same_type_for_distinct_accounts()
        {
            var insightEvent1 = new WalkedEvent{ Event = new InsightEvent
            {
                EventType = RandomString(),
                AccountId = NewGuid()
            }};
            var status1 = new TestEventLogWalkerStatus();

            ClassUnderTest.ProcessEvent(insightEvent1,status1);

            var insightEvent2 = new WalkedEvent{ Event = new InsightEvent
            {
                EventType = insightEvent1.Event.EventType,
                AccountId = NewGuid()
            }};
            var status2 = new TestEventLogWalkerStatus();

            ClassUnderTest.ProcessEvent(insightEvent2,status2);

            Assert.That(ClassUnderTest.EventAccountCounts, Has.Count.EqualTo(1));
            Assert.That(ClassUnderTest.EventAccountCounts, Contains.Key(insightEvent1.Event.EventType));
            Assert.That(ClassUnderTest.EventAccountCounts[insightEvent1.Event.EventType], Has.Count.EqualTo(2));
            Assert.That(ClassUnderTest.EventAccountCounts[insightEvent1.Event.EventType],
                Contains.Item(insightEvent1.Event.AccountId));
            Assert.That(ClassUnderTest.EventAccountCounts[insightEvent1.Event.EventType],
                Contains.Item(insightEvent2.Event.AccountId));
        }

        [Test]
        public void When_processing_multiple_events_of_the_same_type_for_the_same_account()
        {
            var insightEvent1 = new WalkedEvent{ Event = new InsightEvent
            {
                EventType = RandomString(),
                AccountId = NewGuid()
            }};
            var status1 = new TestEventLogWalkerStatus();

            ClassUnderTest.ProcessEvent(insightEvent1,status1);

            var insightEvent2 = new WalkedEvent{ Event = new InsightEvent
            {
                EventType = insightEvent1.Event.EventType,
                AccountId = insightEvent1.Event.AccountId
            }};
            var status2 = new TestEventLogWalkerStatus();

            ClassUnderTest.ProcessEvent(insightEvent2,status2);

            Assert.That(ClassUnderTest.EventAccountCounts, Has.Count.EqualTo(1));
            Assert.That(ClassUnderTest.EventAccountCounts, Contains.Key(insightEvent1.Event.EventType));
            Assert.That(ClassUnderTest.EventAccountCounts[insightEvent1.Event.EventType], Has.Count.EqualTo(1));
            Assert.That(ClassUnderTest.EventAccountCounts[insightEvent1.Event.EventType],
                Contains.Item(insightEvent1.Event.AccountId));
        }

        [Test]
        public void When_processing_multiple_events_of_distinct_types_for_distinct_accounts()
        {
            var insightEvent1 = new WalkedEvent{ Event = new InsightEvent
            {
                EventType = RandomString(),
                AccountId = NewGuid()
            }};
            var status1 = new TestEventLogWalkerStatus();

            ClassUnderTest.ProcessEvent(insightEvent1,status1);

            var insightEvent2 = new WalkedEvent{ Event = new InsightEvent
            {
                EventType = RandomString(),
                AccountId = NewGuid()
            }};
            var status2 = new TestEventLogWalkerStatus();

            ClassUnderTest.ProcessEvent(insightEvent2,status2);

            Assert.That(ClassUnderTest.EventAccountCounts, Has.Count.EqualTo(2));
            Assert.That(ClassUnderTest.EventAccountCounts, Contains.Key(insightEvent1.Event.EventType));
            Assert.That(ClassUnderTest.EventAccountCounts[insightEvent1.Event.EventType], Has.Count.EqualTo(1));
            Assert.That(ClassUnderTest.EventAccountCounts[insightEvent1.Event.EventType],
                Contains.Item(insightEvent1.Event.AccountId));
            Assert.That(ClassUnderTest.EventAccountCounts, Contains.Key(insightEvent2.Event.EventType));
            Assert.That(ClassUnderTest.EventAccountCounts[insightEvent2.Event.EventType], Has.Count.EqualTo(1));
            Assert.That(ClassUnderTest.EventAccountCounts[insightEvent2.Event.EventType],
                Contains.Item(insightEvent2.Event.AccountId));
        }

        [Test]
        public void When_processing_multiple_events_of_the_same_type_for_distinct_user()
        {
            var insightEvent1 = new WalkedEvent{ Event = new InsightEvent
            {
                EventType = RandomString(),
                UserId = NewGuid()
            }};
            var status1 = new TestEventLogWalkerStatus();

            ClassUnderTest.ProcessEvent(insightEvent1,status1);

            var insightEvent2 = new WalkedEvent{ Event = new InsightEvent
            {
                EventType = insightEvent1.Event.EventType,
                UserId = NewGuid()
            }};
            var status2 = new TestEventLogWalkerStatus();

            ClassUnderTest.ProcessEvent(insightEvent2,status2);

            Assert.That(ClassUnderTest.EventUserCounts, Has.Count.EqualTo(1));
            Assert.That(ClassUnderTest.EventUserCounts, Contains.Key(insightEvent1.Event.EventType));
            Assert.That(ClassUnderTest.EventUserCounts[insightEvent1.Event.EventType], Has.Count.EqualTo(2));
            Assert.That(ClassUnderTest.EventUserCounts[insightEvent1.Event.EventType], Contains.Item(insightEvent1.Event.UserId));
            Assert.That(ClassUnderTest.EventUserCounts[insightEvent1.Event.EventType], Contains.Item(insightEvent2.Event.UserId));
        }

        [Test]
        public void When_processing_multiple_events_of_the_same_type_for_the_same_user()
        {
            var insightEvent1 = new WalkedEvent{ Event = new InsightEvent
            {
                EventType = RandomString(),
                UserId = NewGuid()
            }};
            var status1 = new TestEventLogWalkerStatus();

            ClassUnderTest.ProcessEvent(insightEvent1,status1);

            var insightEvent2 = new WalkedEvent{ Event = new InsightEvent
            {
                EventType = insightEvent1.Event.EventType,
                UserId = insightEvent1.Event.UserId
            }};
            var status2 = new TestEventLogWalkerStatus();

            ClassUnderTest.ProcessEvent(insightEvent2,status2);

            Assert.That(ClassUnderTest.EventUserCounts, Has.Count.EqualTo(1));
            Assert.That(ClassUnderTest.EventUserCounts, Contains.Key(insightEvent1.Event.EventType));
            Assert.That(ClassUnderTest.EventUserCounts[insightEvent1.Event.EventType], Has.Count.EqualTo(1));
            Assert.That(ClassUnderTest.EventUserCounts[insightEvent1.Event.EventType], Contains.Item(insightEvent1.Event.UserId));
        }

        [Test]
        public void When_processing_multiple_events_of_distinct_types_for_distinct_users()
        {
            var insightEvent1 = new WalkedEvent{ Event = new InsightEvent
            {
                EventType = RandomString(),
                UserId = NewGuid()
            }};
            var status1 = new TestEventLogWalkerStatus();

            ClassUnderTest.ProcessEvent(insightEvent1,status1);

            var insightEvent2 = new WalkedEvent{ Event = new InsightEvent
            {
                EventType = RandomString(),
                UserId = NewGuid()
            }};
            var status2 = new TestEventLogWalkerStatus();

            ClassUnderTest.ProcessEvent(insightEvent2,status2);

            Assert.That(ClassUnderTest.EventUserCounts, Has.Count.EqualTo(2));
            Assert.That(ClassUnderTest.EventUserCounts, Contains.Key(insightEvent1.Event.EventType));
            Assert.That(ClassUnderTest.EventUserCounts[insightEvent1.Event.EventType], Has.Count.EqualTo(1));
            Assert.That(ClassUnderTest.EventUserCounts[insightEvent1.Event.EventType], Contains.Item(insightEvent1.Event.UserId));
            Assert.That(ClassUnderTest.EventUserCounts, Contains.Key(insightEvent2.Event.EventType));
            Assert.That(ClassUnderTest.EventUserCounts[insightEvent2.Event.EventType], Has.Count.EqualTo(1));
            Assert.That(ClassUnderTest.EventUserCounts[insightEvent2.Event.EventType], Contains.Item(insightEvent2.Event.UserId));
        }

        [Test]
        public void When_processing_and_persist_state_should_occur()
        {
            var insightEvent1 = new WalkedEvent{ Event = new InsightEvent
            {
                EventType = RandomString(),
                UserId = NewGuid()
            }};
            var resumeToken = RandomString();
            var stateJson = RandomString();

            var statusMock = GetMock<IEventLogWalkerStatus>();
            statusMock.SetupGet(x => x.PageStatus).Returns(PageStatus.Start);
            statusMock.Setup(x => x.GetResumeToken()).Returns(resumeToken);

            AccountUserCountsReportState capturedAccountUserCountsReportState = null;
            GetMock<IJsonSerializer>().Setup(x => x.Serialize(IsAny<AccountUserCountsReportState>()))
                .Callback<AccountUserCountsReportState>(reportState => capturedAccountUserCountsReportState = reportState)
                .Returns(stateJson);

            ClassUnderTest.ProcessEvent(insightEvent1,statusMock.Object);

            GetMock<IFileSystem>().Verify(x => x.WriteFile(AccountUserCountsReport.StateFilePath, stateJson));

            Assert.That(capturedAccountUserCountsReportState.EventAccountCounts, Is.SameAs(ClassUnderTest.EventAccountCounts));
            Assert.That(capturedAccountUserCountsReportState.EventUserCounts, Is.SameAs(ClassUnderTest.EventUserCounts));
            Assert.That(capturedAccountUserCountsReportState.WalkerResumeToken, Is.EqualTo(resumeToken));
        }

        [Test]
        public void When_processing_state_should_not_be_persisted_for_each_event()
        {
            var insightEvent1 = new WalkedEvent{ Event = new InsightEvent
            {
                EventType = RandomString(),
                UserId = NewGuid()
            }};

            var statusMock = GetMock<IEventLogWalkerStatus>();
            statusMock.SetupGet(x => x.TotalEventsProcessed).Returns(499);
            statusMock.SetupGet(x => x.PageEventsCount).Returns(1000);

            ClassUnderTest.ProcessEvent(insightEvent1,statusMock.Object);

            statusMock.VerifyNever(x => x.GetResumeToken());
            GetMock<IJsonSerializer>().VerifyNever(x => x.Serialize(IsAny<AccountUserCountsReportState>()));
            GetMock<IFileSystem>().VerifyNever(x => x.WriteFile(IsAny<string>(), IsAny<string>()));
        }

        public class TestEventLogWalkerStatus : IEventLogWalkerStatus
        {
            public int TotalEventsProcessed { get; set; }
            public int PageNumber { get; set; }
            public int PageEventIndex { get; set; }
            public int PageEventsCount { get; set; }
            public Exception Exception { get; set; }
            public PageStatus PageStatus { get; set; }
            public string GetResumeToken()
            {
                return Guid.NewGuid().ToString();
            }
        }
    }
}
