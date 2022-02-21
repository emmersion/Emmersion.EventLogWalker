using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Emmersion.Testing;
using Moq;
using NUnit.Framework;

namespace Emmersion.EventLogWalker.UnitTests
{
    internal class EventLogWalkerTests : With_an_automocked<EventLogWalker>
    {
        [Test]
        public void When_constructing_default_args()
        {
            var walkArgs = new WalkArgs<InsightEvent>();

            Assert.That(walkArgs.StartInclusive, Is.EqualTo(DateTimeOffset.MinValue));
            Assert.That(walkArgs.EndExclusive, Is.EqualTo(DateTimeOffset.MaxValue));
        }

        [Test]
        public async Task When_walking_the_event_log_for_multiple_pages_async()
        {
            var walkArgs = new WalkArgs<InsightEvent>
            {
                Pager = GetMock<IPager<InsightEvent>>().Object,
                StartInclusive = DateTimeOffset.UtcNow.AddDays(-7),
                EndExclusive = DateTimeOffset.UtcNow,
            };
            var initialState = new WalkState<InsightEvent>
            {
                Cursor = new Cursor(),
                Events = new List<InsightEvent>
                {
                    new InsightEvent(),
                    new InsightEvent()
                }
            };
            var state2 = new WalkState<InsightEvent>();
            var state3 = new WalkState<InsightEvent>
            {
                Events = new List<InsightEvent>
                {
                    new InsightEvent(),
                    new InsightEvent()
                },
                Cursor = new Cursor()
            };
            var state4 = new WalkState<InsightEvent>();
            var finalState = new WalkState<InsightEvent>
            {
                Events = new List<InsightEvent>(),
                Cursor = null
            };
            var eventProcessorFuncWasCalled = false;
            Func<InsightEvent, IEventLogWalkerStatus, Task> eventProcessorFunc = (xEvent, xStatus) =>
            {
                eventProcessorFuncWasCalled = true;
                return Task.CompletedTask;
            };
            IEventProcessor<InsightEvent> capturedEventProcessor = null;

            GetMock<IStateLoader>()
                .Setup(x => x.LoadInitialStateAsync(walkArgs.Pager, walkArgs.StartInclusive, walkArgs.EndExclusive, null))
                .ReturnsAsync(initialState);
            GetMock<IStateProcessor>().Setup(x => x.ProcessStateAsync(IsAny<IEventProcessor<InsightEvent>>(), initialState))
                .Callback<IEventProcessor<InsightEvent>, WalkState<InsightEvent>>((eventProcessor, _) => capturedEventProcessor = eventProcessor)
                .ReturnsAsync(state2);
            GetMock<IStateLoader>().Setup(x => x.LoadNextStateAsync(walkArgs.Pager, state2))
                .ReturnsAsync(state3);
            GetMock<IStateProcessor>().Setup(x => x.ProcessStateAsync(IsAny<IEventProcessor<InsightEvent>>(), state3))
                .ReturnsAsync(state4);
            GetMock<IStateLoader>().Setup(x => x.LoadNextStateAsync(walkArgs.Pager, state4))
                .ReturnsAsync(finalState);

            var status = await ClassUnderTest.WalkAsync(walkArgs, eventProcessorFunc);
            await capturedEventProcessor.ProcessEventAsync(null, null);

            GetMock<IResourceThrottle>().Verify(x => x.WaitForNextAccessAsync(), Times.Exactly(2));
            GetMock<IResourceThrottle>().VerifySet(x => x.LastAccess = IsAny<DateTimeOffset>(), Times.Exactly(2));
            GetMock<IResourceThrottle>().VerifySet(x => x.MinimumDurationBetweenAccess = IsAny<TimeSpan>(), Times.Once);
            GetMock<IResourceThrottle>().Verify(x => x.WaitForNextAccessAsync(), Times.Exactly(2));

            var statusStateFieldInfo =
                status.GetType().GetField("state", BindingFlags.NonPublic | BindingFlags.Instance);
            var privateStateOfStatus = (WalkState<InsightEvent>) statusStateFieldInfo?.GetValue(status);
            Assert.That(privateStateOfStatus, Is.SameAs(finalState));

            Assert.That(eventProcessorFuncWasCalled, Is.True);
        }

        [Test]
        public async Task When_walking_the_event_log_synchronously()
        {
            var walkArgs = new WalkArgs<InsightEvent>
            {
                Pager = GetMock<IPager<InsightEvent>>().Object,
                StartInclusive = DateTimeOffset.UtcNow.AddDays(-7),
                EndExclusive = DateTimeOffset.UtcNow,
            };
            var initialState = new WalkState<InsightEvent>
            {
                Cursor = new Cursor(),
                Events = new List<InsightEvent>
                {
                    new InsightEvent(),
                    new InsightEvent()
                }
            };
            var state2 = new WalkState<InsightEvent>
            {
                Exception = new Exception(RandomString())
            };
            var eventProcessorFuncWasCalled = false;
            Action<InsightEvent, IEventLogWalkerStatus> eventProcessorFunc = (xEvent, xStatus) =>
            {
                eventProcessorFuncWasCalled = true;
            };
            IEventProcessor<InsightEvent> capturedEventProcessor = null;

            GetMock<IStateLoader>()
                .Setup(x => x.LoadInitialStateAsync(walkArgs.Pager, walkArgs.StartInclusive, walkArgs.EndExclusive, null))
                .ReturnsAsync(initialState);
            GetMock<IStateProcessor>().Setup(x => x.ProcessStateAsync(IsAny<IEventProcessor<InsightEvent>>(), initialState))
                .Callback<IEventProcessor<InsightEvent>, WalkState<InsightEvent>>((eventProcessor, _) => capturedEventProcessor = eventProcessor)
                .ReturnsAsync(state2);

            await ClassUnderTest.WalkAsync(walkArgs, eventProcessorFunc);
            await capturedEventProcessor.ProcessEventAsync(null, null);

            Assert.That(eventProcessorFuncWasCalled, Is.True);
        }

        [Test]
        public async Task When_resuming_walking_the_event_log()
        {
            var walkArgs = new WalkArgs<InsightEvent>
            {
                Pager = GetMock<IPager<InsightEvent>>().Object,
                StartInclusive = DateTimeOffset.UtcNow.AddDays(-7),
                EndExclusive = DateTimeOffset.UtcNow,
                ResumeToken = RandomString()
            };
            var resumingInitialState = new WalkState<InsightEvent>
            {
                Cursor = null,
                Events = new List<InsightEvent>
                {
                    new InsightEvent(),
                    new InsightEvent()
                }
            };

            var state1 = new WalkState<InsightEvent>();
            var finalState = new WalkState<InsightEvent>
            {
                Events = new List<InsightEvent>()
            };

            GetMock<IStateLoader>().Setup(x =>
                    x.LoadInitialStateAsync(walkArgs.Pager, walkArgs.StartInclusive, walkArgs.EndExclusive, walkArgs.ResumeToken))
                .ReturnsAsync(resumingInitialState);
            GetMock<IStateProcessor>().Setup(x => x.ProcessStateAsync(IsAny<IEventProcessor<InsightEvent>>(), resumingInitialState))
                .ReturnsAsync(state1);
            GetMock<IStateLoader>().Setup(x => x.LoadNextStateAsync(walkArgs.Pager, state1))
                .ReturnsAsync(finalState);

            await ClassUnderTest.WalkAsync(walkArgs, (x, y) => { });
        }

        [Test]
        public async Task When_walking_the_event_log_and_an_error_occurs_while_loading_initial_state()
        {
            var walkArgs = new WalkArgs<InsightEvent>
            {
                Pager = GetMock<IPager<InsightEvent>>().Object,
                StartInclusive = DateTimeOffset.UtcNow.AddDays(-7),
                EndExclusive = DateTimeOffset.UtcNow,
            };

            var initialStateWithError = new WalkState<InsightEvent>
            {
                Cursor = new Cursor(),
                Events = new List<InsightEvent>(),
                Exception = new Exception(RandomString())
            };

            GetMock<IStateLoader>()
                .Setup(x => x.LoadInitialStateAsync(walkArgs.Pager, walkArgs.StartInclusive, walkArgs.EndExclusive, null))
                .ReturnsAsync(initialStateWithError);

            var status = await ClassUnderTest.WalkAsync(walkArgs, (x, y) => { });

            var statusStateFieldInfo =
                status.GetType().GetField("state", BindingFlags.NonPublic | BindingFlags.Instance);
            var privateStateOfStatus = (WalkState<InsightEvent>) statusStateFieldInfo?.GetValue(status);
            Assert.That(privateStateOfStatus, Is.SameAs(initialStateWithError));
        }

        [Test]
        public async Task When_walking_the_event_log_and_an_error_occurs_while_processing_state()
        {
            var walkArgs = new WalkArgs<InsightEvent>
            {
                Pager = GetMock<IPager<InsightEvent>>().Object,
                StartInclusive = DateTimeOffset.UtcNow.AddDays(-7),
                EndExclusive = DateTimeOffset.UtcNow,
            };

            var initialState = new WalkState<InsightEvent>
            {
                Cursor = new Cursor(),
                Events = new List<InsightEvent>
                {
                    new InsightEvent(),
                    new InsightEvent()
                }
            };

            var stateWithError = new WalkState<InsightEvent>
            {
                Exception = new Exception(RandomString())
            };

            GetMock<IStateLoader>()
                .Setup(x => x.LoadInitialStateAsync(walkArgs.Pager, walkArgs.StartInclusive, walkArgs.EndExclusive, null))
                .ReturnsAsync(initialState);
            GetMock<IStateProcessor>().Setup(x => x.ProcessStateAsync(IsAny<IEventProcessor<InsightEvent>>(), initialState))
                .ReturnsAsync(stateWithError);

            var status = await ClassUnderTest.WalkAsync(walkArgs, (x, y) => { });

            var statusStateFieldInfo =
                status.GetType().GetField("state", BindingFlags.NonPublic | BindingFlags.Instance);
            var privateStateOfStatus = (WalkState<InsightEvent>) statusStateFieldInfo?.GetValue(status);
            Assert.That(privateStateOfStatus, Is.SameAs(stateWithError));
        }

        [Test]
        public async Task When_walking_the_event_log_and_an_error_occurs_while_processing_next_state()
        {
            var walkArgs = new WalkArgs<InsightEvent>
            {
                Pager = GetMock<IPager<InsightEvent>>().Object,
                StartInclusive = DateTimeOffset.UtcNow.AddDays(-7),
                EndExclusive = DateTimeOffset.UtcNow,
            };

            var initialState = new WalkState<InsightEvent>
            {
                Cursor = new Cursor(),
                Events = new List<InsightEvent>
                {
                    new InsightEvent(),
                    new InsightEvent()
                }
            };

            var state2 = new WalkState<InsightEvent>
            {
                Cursor = new Cursor(),
                Events = new List<InsightEvent>
                {
                    new InsightEvent(),
                    new InsightEvent()
                }
            };

            var stateWithError = new WalkState<InsightEvent>
            {
                Exception = new Exception(RandomString()),
                Cursor = state2.Cursor,
                Events = state2.Events
            };

            GetMock<IStateLoader>()
                .Setup(x => x.LoadInitialStateAsync(walkArgs.Pager, walkArgs.StartInclusive, walkArgs.EndExclusive, null))
                .ReturnsAsync(initialState);
            GetMock<IStateProcessor>().Setup(x => x.ProcessStateAsync(IsAny<IEventProcessor<InsightEvent>>(), initialState))
                .ReturnsAsync(state2);
            GetMock<IStateLoader>().Setup(x => x.LoadNextStateAsync(walkArgs.Pager, state2))
                .ReturnsAsync(stateWithError);

            var status = await ClassUnderTest.WalkAsync(walkArgs, (x, y) => { });

            GetMock<IStateProcessor>().Verify(x => x.ProcessStateAsync(IsAny<IEventProcessor<InsightEvent>>(), IsAny<WalkState<InsightEvent>>()),
                Times.Once);

            var statusStateFieldInfo =
                status.GetType().GetField("state", BindingFlags.NonPublic | BindingFlags.Instance);
            var privateStateOfStatus = (WalkState<InsightEvent>) statusStateFieldInfo?.GetValue(status);
            Assert.That(privateStateOfStatus, Is.SameAs(stateWithError));
        }
    }
}
