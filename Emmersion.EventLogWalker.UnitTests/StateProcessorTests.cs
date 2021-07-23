using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Emmersion.Testing;
using NUnit.Framework;

namespace Emmersion.EventLogWalker.UnitTests
{
    public class StateProcessorTests : With_an_automocked<StateProcessor>
    {
        [Test]
        public async Task When_processing_state()
        {
            var pageNumber = 1;
            var initialState = new WalkState
            {
                PageNumber = pageNumber,
                Events = new List<InsightEvent>
                {
                    new InsightEvent(),
                    new InsightEvent()
                },
                Cursor = new Cursor(),
                PreviousCursor = new Cursor(),
                TotalEventsProcessed = 0,
                PageEventIndex = 0
            };
            IEventLogWalkerStatus capturedStatus1 = null;
            IEventLogWalkerStatus capturedStatus2 = null;

            var mockEventProcessor = GetMock<IEventProcessor>();

            mockEventProcessor.Setup(x => x.ProcessEventAsync(initialState.Events[0], IsAny<IEventLogWalkerStatus>()))
                .Callback<InsightEvent, IEventLogWalkerStatus>((_, status) => capturedStatus1 = status);
            mockEventProcessor.Setup(x => x.ProcessEventAsync(initialState.Events[1], IsAny<IEventLogWalkerStatus>()))
                .Callback<InsightEvent, IEventLogWalkerStatus>((_, status) => capturedStatus2 = status);

            var finalState =
                await ClassUnderTest.ProcessStateAsync(mockEventProcessor.Object, initialState);

            GetMock<IJsonSerializer>().VerifyNever(x => x.Serialize(IsAny<WalkState>()));

            Assert.That(capturedStatus1.PageEventIndex, Is.EqualTo(0));
            Assert.That(capturedStatus1.PageNumber, Is.EqualTo(initialState.PageNumber));
            Assert.That(capturedStatus1.TotalEventsProcessed, Is.EqualTo(0));
            Assert.That(capturedStatus1.PageEventsCount, Is.EqualTo(initialState.Events.Count));
            Assert.That(capturedStatus1.PageStatus, Is.EqualTo(PageStatus.Start));
            Assert.That(GetPrivateStateOfStatus(capturedStatus1).PreviousCursor, Is.EqualTo(initialState.PreviousCursor));

            Assert.That(capturedStatus2.PageEventIndex, Is.EqualTo(1));
            Assert.That(capturedStatus2.PageNumber, Is.EqualTo(initialState.PageNumber));
            Assert.That(capturedStatus2.TotalEventsProcessed, Is.EqualTo(1));
            Assert.That(capturedStatus2.PageEventsCount, Is.EqualTo(initialState.Events.Count));
            Assert.That(GetPrivateStateOfStatus(capturedStatus2).PreviousCursor, Is.EqualTo(initialState.PreviousCursor));

            Assert.That(finalState.Exception, Is.Null);
            Assert.That(finalState.PageEventIndex, Is.EqualTo(initialState.Events.Count));
            Assert.That(finalState.TotalEventsProcessed, Is.EqualTo(2));
            Assert.That(finalState.PageNumber, Is.EqualTo(initialState.PageNumber));
            Assert.That(finalState.Events, Is.EqualTo(initialState.Events));
            Assert.That(finalState.Cursor, Is.EqualTo(initialState.Cursor));
            Assert.That(finalState.PreviousCursor, Is.EqualTo(initialState.PreviousCursor));
        }

        [Test]
        public async Task When_resuming_state_processing()
        {
            var initialState = new WalkState
            {
                Events = new List<InsightEvent>
                {
                    new InsightEvent(),
                    new InsightEvent()
                },
                PageNumber = 2,
                Cursor = new Cursor(),
                PreviousCursor = new Cursor(),
                PageEventIndex = 1,
                TotalEventsProcessed = 3
            };
            IEventLogWalkerStatus capturedStatus = null;

            var mockEventProcessor = GetMock<IEventProcessor>();

            mockEventProcessor.Setup(x => x.ProcessEventAsync(initialState.Events[1], IsAny<IEventLogWalkerStatus>()))
                .Callback<InsightEvent, IEventLogWalkerStatus>((_, status) => capturedStatus = status);

            var finalState =
                await ClassUnderTest.ProcessStateAsync(mockEventProcessor.Object, initialState);

            Assert.That(capturedStatus.PageEventIndex, Is.EqualTo(initialState.PageEventIndex));
            Assert.That(capturedStatus.PageNumber, Is.EqualTo(initialState.PageNumber));
            Assert.That(capturedStatus.TotalEventsProcessed, Is.EqualTo(initialState.TotalEventsProcessed));
            Assert.That(capturedStatus.PageEventsCount, Is.EqualTo(initialState.Events.Count));

            Assert.That(finalState.PageEventIndex, Is.EqualTo(initialState.Events.Count));
            Assert.That(finalState.TotalEventsProcessed, Is.EqualTo(4));
            Assert.That(finalState.PageNumber, Is.EqualTo(initialState.PageNumber));
        }

        [Test]
        public async Task When_processing_an_event_throws()
        {
            var exception = new Exception();
            var initialState = new WalkState
            {
                Events = new List<InsightEvent>
                {
                    new InsightEvent(),
                    new InsightEvent()
                },
                Cursor = new Cursor(),
                PreviousCursor = new Cursor(),
                PageEventIndex = 0
            };

            IEventLogWalkerStatus capturedStatus = null;
            var mockEventProcessor = GetMock<IEventProcessor>();
            mockEventProcessor.Setup(x => x.ProcessEventAsync(initialState.Events[0], IsAny<IEventLogWalkerStatus>()))
                .Callback<InsightEvent, IEventLogWalkerStatus>((_, status) => capturedStatus = status)
                .Throws(exception);

            var finalState = await ClassUnderTest.ProcessStateAsync(mockEventProcessor.Object, initialState);

            Assert.That(capturedStatus.PageEventIndex, Is.EqualTo(0));
            Assert.That(capturedStatus.PageNumber, Is.EqualTo(initialState.PageNumber));
            Assert.That(capturedStatus.TotalEventsProcessed, Is.EqualTo(0));
            Assert.That(capturedStatus.PageEventsCount, Is.EqualTo(initialState.Events.Count));
            Assert.That(capturedStatus.PageStatus, Is.EqualTo(PageStatus.Start));

            Assert.That(finalState.Exception, Is.EqualTo(exception));
            Assert.That(finalState.PageEventIndex, Is.EqualTo(0));
            Assert.That(finalState.TotalEventsProcessed, Is.EqualTo(0));
            Assert.That(finalState.PageNumber, Is.EqualTo(initialState.PageNumber));
            Assert.That(finalState.Events, Is.EqualTo(initialState.Events));
            Assert.That(finalState.Cursor, Is.EqualTo(initialState.Cursor));
            Assert.That(finalState.PreviousCursor, Is.EqualTo(initialState.PreviousCursor));
        }

        private WalkState GetPrivateStateOfStatus(IEventLogWalkerStatus status)
        {
            var stateFieldInfo = typeof(EventLogWalkerStatus).GetField("state", BindingFlags.Instance | BindingFlags.NonPublic);

            return (WalkState)stateFieldInfo!.GetValue(status);
        }
    }
}
