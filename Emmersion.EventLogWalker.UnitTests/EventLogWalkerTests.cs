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
            var walkArgs = new WalkArgs();

            Assert.That(walkArgs.StartInclusive, Is.EqualTo(DateTimeOffset.MinValue));
            Assert.That(walkArgs.EndExclusive, Is.EqualTo(DateTimeOffset.MaxValue));
        }

        [Test]
        public async Task When_walking_the_event_log_for_multiple_pages_async()
        {
            var walkArgs = new WalkArgs
            {
                StartInclusive = DateTimeOffset.UtcNow.AddDays(-7),
                EndExclusive = DateTimeOffset.UtcNow,
            };
            var initialState = new WalkState
            {
                Cursor = new Cursor(),
                Events = new List<WalkedEvent>
                {
                    new WalkedEvent(),
                    new WalkedEvent()
                }
            };
            var state2 = new WalkState();
            var state3 = new WalkState
            {
                Events = new List<WalkedEvent>
                {
                    new WalkedEvent(),
                    new WalkedEvent()
                },
                Cursor = new Cursor()
            };
            var state4 = new WalkState();
            var finalState = new WalkState
            {
                Events = new List<WalkedEvent>(),
                Cursor = null
            };
            var eventProcessorFuncWasCalled = false;
            Func<WalkedEvent, IEventLogWalkerStatus, Task> eventProcessorFunc = (xEvent, xStatus) =>
            {
                eventProcessorFuncWasCalled = true;
                return Task.CompletedTask;
            };
            IEventProcessor capturedEventProcessor = null;

            GetMock<IStateLoader>()
                .Setup(x => x.LoadInitialStateAsync(walkArgs.StartInclusive, walkArgs.EndExclusive, null))
                .ReturnsAsync(initialState);
            GetMock<IStateProcessor>().Setup(x => x.ProcessStateAsync(IsAny<IEventProcessor>(), initialState))
                .Callback<IEventProcessor, WalkState>((eventProcessor, _) => capturedEventProcessor = eventProcessor)
                .ReturnsAsync(state2);
            GetMock<IStateLoader>().Setup(x => x.LoadNextStateAsync(state2))
                .ReturnsAsync(state3);
            GetMock<IStateProcessor>().Setup(x => x.ProcessStateAsync(IsAny<IEventProcessor>(), state3))
                .ReturnsAsync(state4);
            GetMock<IStateLoader>().Setup(x => x.LoadNextStateAsync(state4))
                .ReturnsAsync(finalState);

            var status = await ClassUnderTest.WalkAsync(walkArgs, eventProcessorFunc);
            await capturedEventProcessor.ProcessEventAsync(null, null);

            GetMock<IResourceThrottle>().Verify(x => x.WaitForNextAccessAsync(), Times.Exactly(2));
            GetMock<IResourceThrottle>().VerifySet(x => x.LastAccess = IsAny<DateTimeOffset>(), Times.Exactly(2));
            GetMock<IResourceThrottle>().VerifySet(x => x.MinimumDurationBetweenAccess = IsAny<TimeSpan>(), Times.Once);
            GetMock<IResourceThrottle>().Verify(x => x.WaitForNextAccessAsync(), Times.Exactly(2));

            var statusStateFieldInfo =
                status.GetType().GetField("state", BindingFlags.NonPublic | BindingFlags.Instance);
            var privateStateOfStatus = (WalkState) statusStateFieldInfo?.GetValue(status);
            Assert.That(privateStateOfStatus, Is.SameAs(finalState));

            Assert.That(eventProcessorFuncWasCalled, Is.True);
        }

        [Test]
        public async Task When_walking_the_event_log_synchronously()
        {
            var walkArgs = new WalkArgs
            {
                StartInclusive = DateTimeOffset.UtcNow.AddDays(-7),
                EndExclusive = DateTimeOffset.UtcNow,
            };
            var initialState = new WalkState
            {
                Cursor = new Cursor(),
                Events = new List<WalkedEvent>
                {
                    new WalkedEvent(),
                    new WalkedEvent()
                }
            };
            var state2 = new WalkState
            {
                Exception = new Exception(RandomString())
            };
            var eventProcessorFuncWasCalled = false;
            Action<WalkedEvent, IEventLogWalkerStatus> eventProcessorFunc = (xEvent, xStatus) =>
            {
                eventProcessorFuncWasCalled = true;
            };
            IEventProcessor capturedEventProcessor = null;

            GetMock<IStateLoader>()
                .Setup(x => x.LoadInitialStateAsync(walkArgs.StartInclusive, walkArgs.EndExclusive, null))
                .ReturnsAsync(initialState);
            GetMock<IStateProcessor>().Setup(x => x.ProcessStateAsync(IsAny<IEventProcessor>(), initialState))
                .Callback<IEventProcessor, WalkState>((eventProcessor, _) => capturedEventProcessor = eventProcessor)
                .ReturnsAsync(state2);

            await ClassUnderTest.WalkAsync(walkArgs, eventProcessorFunc);
            await capturedEventProcessor.ProcessEventAsync(null, null);

            Assert.That(eventProcessorFuncWasCalled, Is.True);
        }

        [Test]
        public async Task When_resuming_walking_the_event_log()
        {
            var walkArgs = new WalkArgs
            {
                StartInclusive = DateTimeOffset.UtcNow.AddDays(-7),
                EndExclusive = DateTimeOffset.UtcNow,
                ResumeToken = RandomString()
            };
            var resumingInitialState = new WalkState
            {
                Cursor = null,
                Events = new List<WalkedEvent>
                {
                    new WalkedEvent(),
                    new WalkedEvent()
                }
            };

            var state1 = new WalkState();
            var finalState = new WalkState
            {
                Events = new List<WalkedEvent>()
            };

            GetMock<IStateLoader>().Setup(x =>
                    x.LoadInitialStateAsync(walkArgs.StartInclusive, walkArgs.EndExclusive, walkArgs.ResumeToken))
                .ReturnsAsync(resumingInitialState);
            GetMock<IStateProcessor>().Setup(x => x.ProcessStateAsync(IsAny<IEventProcessor>(), resumingInitialState))
                .ReturnsAsync(state1);
            GetMock<IStateLoader>().Setup(x => x.LoadNextStateAsync(state1))
                .ReturnsAsync(finalState);

            await ClassUnderTest.WalkAsync(walkArgs, (x, y) => { });
        }

        [Test]
        public async Task When_walking_the_event_log_and_an_error_occurs_while_loading_initial_state()
        {
            var walkArgs = new WalkArgs
            {
                StartInclusive = DateTimeOffset.UtcNow.AddDays(-7),
                EndExclusive = DateTimeOffset.UtcNow,
            };

            var initialStateWithError = new WalkState
            {
                Cursor = new Cursor(),
                Events = new List<WalkedEvent>(),
                Exception = new Exception(RandomString())
            };

            GetMock<IStateLoader>()
                .Setup(x => x.LoadInitialStateAsync(walkArgs.StartInclusive, walkArgs.EndExclusive, null))
                .ReturnsAsync(initialStateWithError);

            var status = await ClassUnderTest.WalkAsync(walkArgs, (x, y) => { });

            var statusStateFieldInfo =
                status.GetType().GetField("state", BindingFlags.NonPublic | BindingFlags.Instance);
            var privateStateOfStatus = (WalkState) statusStateFieldInfo?.GetValue(status);
            Assert.That(privateStateOfStatus, Is.SameAs(initialStateWithError));
        }

        [Test]
        public async Task When_walking_the_event_log_and_an_error_occurs_while_processing_state()
        {
            var walkArgs = new WalkArgs
            {
                StartInclusive = DateTimeOffset.UtcNow.AddDays(-7),
                EndExclusive = DateTimeOffset.UtcNow,
            };

            var initialState = new WalkState
            {
                Cursor = new Cursor(),
                Events = new List<WalkedEvent>
                {
                    new WalkedEvent(),
                    new WalkedEvent()
                }
            };

            var stateWithError = new WalkState
            {
                Exception = new Exception(RandomString())
            };

            GetMock<IStateLoader>()
                .Setup(x => x.LoadInitialStateAsync(walkArgs.StartInclusive, walkArgs.EndExclusive, null))
                .ReturnsAsync(initialState);
            GetMock<IStateProcessor>().Setup(x => x.ProcessStateAsync(IsAny<IEventProcessor>(), initialState))
                .ReturnsAsync(stateWithError);

            var status = await ClassUnderTest.WalkAsync(walkArgs, (x, y) => { });

            var statusStateFieldInfo =
                status.GetType().GetField("state", BindingFlags.NonPublic | BindingFlags.Instance);
            var privateStateOfStatus = (WalkState) statusStateFieldInfo?.GetValue(status);
            Assert.That(privateStateOfStatus, Is.SameAs(stateWithError));
        }

        [Test]
        public async Task When_walking_the_event_log_and_an_error_occurs_while_processing_next_state()
        {
            var walkArgs = new WalkArgs
            {
                StartInclusive = DateTimeOffset.UtcNow.AddDays(-7),
                EndExclusive = DateTimeOffset.UtcNow,
            };

            var initialState = new WalkState
            {
                Cursor = new Cursor(),
                Events = new List<WalkedEvent>
                {
                    new WalkedEvent(),
                    new WalkedEvent()
                }
            };

            var state2 = new WalkState
            {
                Cursor = new Cursor(),
                Events = new List<WalkedEvent>
                {
                    new WalkedEvent(),
                    new WalkedEvent()
                }
            };

            var stateWithError = new WalkState
            {
                Exception = new Exception(RandomString()),
                Cursor = state2.Cursor,
                Events = state2.Events
            };

            GetMock<IStateLoader>()
                .Setup(x => x.LoadInitialStateAsync(walkArgs.StartInclusive, walkArgs.EndExclusive, null))
                .ReturnsAsync(initialState);
            GetMock<IStateProcessor>().Setup(x => x.ProcessStateAsync(IsAny<IEventProcessor>(), initialState))
                .ReturnsAsync(state2);
            GetMock<IStateLoader>().Setup(x => x.LoadNextStateAsync(state2))
                .ReturnsAsync(stateWithError);

            var status = await ClassUnderTest.WalkAsync(walkArgs, (x, y) => { });

            GetMock<IStateProcessor>().Verify(x => x.ProcessStateAsync(IsAny<IEventProcessor>(), IsAny<WalkState>()),
                Times.Once);

            var statusStateFieldInfo =
                status.GetType().GetField("state", BindingFlags.NonPublic | BindingFlags.Instance);
            var privateStateOfStatus = (WalkState) statusStateFieldInfo?.GetValue(status);
            Assert.That(privateStateOfStatus, Is.SameAs(stateWithError));
        }
    }
}
