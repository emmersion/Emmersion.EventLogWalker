using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Emmersion.Testing;
using Moq;
using NUnit.Framework;

namespace Emmersion.EventLogWalker.UnitTests
{
    internal class StateLoaderTests: With_an_automocked<StateLoader>
    {
        [Test]
        public async Task When_loading_initial_state_and_not_resuming()
        {
            var startInclusive = DateTimeOffset.UtcNow.AddDays(-1);
            var endExclusive = DateTimeOffset.UtcNow;

            var page = new Page
            {
                NextPage = new Cursor(),
                Events = new List<WalkedEvent>
                {
                    new WalkedEvent(),
                    new WalkedEvent()
                }
            };
            Cursor capturedCursor = null;

            GetMock<IPager>().Setup(x => x.GetPageAsync(IsAny<Cursor>()))
                .Callback<Cursor>(cursor => capturedCursor = cursor)
                .ReturnsAsync(page);

            var state = await ClassUnderTest.LoadInitialStateAsync(startInclusive, endExclusive, null);

            Assert.That(capturedCursor.StartInclusive, Is.EqualTo(startInclusive));
            Assert.That(capturedCursor.EndExclusive, Is.EqualTo(endExclusive));
            Assert.That(state.Cursor, Is.EqualTo(page.NextPage));
            Assert.That(state.PreviousCursor, Is.EqualTo(capturedCursor));
            Assert.That(state.Events, Is.EqualTo(page.Events));
            Assert.That(state.PageEventIndex, Is.EqualTo(0));
            Assert.That(state.PageNumber, Is.EqualTo(1));
            Assert.That(state.TotalEventsProcessed, Is.EqualTo(0));
        }

        [Test]
        public async Task When_loading_initial_state_while_resuming()
        {
            var startInclusive = DateTimeOffset.UtcNow.AddDays(-1);
            var endExclusive = DateTimeOffset.UtcNow;

            var resumeToken = new ResumeToken
            {
                Cursor = new Cursor(),
                PageEventIndex = 1,
                PageNumber = 2,
                TotalProcessedEvents = 3
            };
            var resumeTokenJson = RandomString();
            var page = new Page
            {
                Events = new List<WalkedEvent>
                {
                    new WalkedEvent(),
                    new WalkedEvent()
                },
                NextPage = new Cursor()
            };

            GetMock<IJsonSerializer>().Setup(x => x.Deserialize<ResumeToken>(resumeTokenJson))
                .Returns(resumeToken);
            GetMock<IPager>().Setup(x => x.GetPageAsync(resumeToken.Cursor))
                .ReturnsAsync(page);

            var state = await ClassUnderTest.LoadInitialStateAsync(startInclusive, endExclusive, resumeTokenJson);

            Assert.That(state.Events, Is.EqualTo(page.Events));
            Assert.That(state.Cursor, Is.EqualTo(page.NextPage));
            Assert.That(state.PreviousCursor, Is.EqualTo(resumeToken.Cursor));
            Assert.That(state.PageEventIndex, Is.EqualTo(resumeToken.PageEventIndex));
            Assert.That(state.PageNumber, Is.EqualTo(resumeToken.PageNumber));
            Assert.That(state.TotalEventsProcessed, Is.EqualTo(resumeToken.TotalProcessedEvents));
        }

        [Test]
        public async Task When_loading_initial_state_while_resuming_after_an_initial_state_failure()
        {
            var startInclusive = DateTimeOffset.UtcNow.AddDays(-1);
            var endExclusive = DateTimeOffset.UtcNow;

            var resumeToken = new ResumeToken
            {
                Cursor = null,
                PageEventIndex = 0,
                PageNumber = 1,
                TotalProcessedEvents = 0
            };
            var resumeTokenJson = RandomString();
            var page = new Page
            {
                Events = new List<WalkedEvent>
                {
                    new WalkedEvent(),
                    new WalkedEvent()
                },
                NextPage = new Cursor()
            };

            GetMock<IJsonSerializer>().Setup(x => x.Deserialize<ResumeToken>(resumeTokenJson))
                .Returns(resumeToken);
            Cursor capturedCursor = null;
            GetMock<IPager>().Setup(x => x.GetPageAsync(IsAny<Cursor>()))
                .Callback<Cursor>(cursor => capturedCursor = cursor)
                .ReturnsAsync(page);

            var state = await ClassUnderTest.LoadInitialStateAsync(startInclusive, endExclusive, resumeTokenJson);

            Assert.That(capturedCursor, Is.Not.Null);
            Assert.That(state.Events, Is.EqualTo(page.Events));
            Assert.That(state.Cursor, Is.EqualTo(page.NextPage));
            Assert.That(state.PreviousCursor, Is.EqualTo(capturedCursor));
            Assert.That(state.PageEventIndex, Is.EqualTo(resumeToken.PageEventIndex));
            Assert.That(state.PageNumber, Is.EqualTo(resumeToken.PageNumber));
            Assert.That(state.TotalEventsProcessed, Is.EqualTo(resumeToken.TotalProcessedEvents));
        }
        
        [Test]
        public async Task When_loading_initial_state_and_an_error_occurs()
        {
            var exception = new Exception(RandomString());
            var start = DateTimeOffset.UtcNow.AddDays(-1);
            var end = DateTimeOffset.UtcNow;

            GetMock<IPager>().Setup(x => x.GetPageAsync(IsAny<Cursor>())).Throws(exception);

            var state = await ClassUnderTest.LoadInitialStateAsync(start, end, null);

            Assert.That(state.Exception, Is.EqualTo(exception));
            Assert.That(state.Cursor.StartInclusive, Is.EqualTo(start));
            Assert.That(state.Cursor.EndExclusive, Is.EqualTo(end));
            Assert.That(state.PreviousCursor, Is.Null);
            Assert.That(state.Events, Is.Empty);
            Assert.That(state.PageEventIndex, Is.EqualTo(0));
            Assert.That(state.TotalEventsProcessed, Is.EqualTo(0));
            Assert.That(state.PageNumber, Is.EqualTo(1));
        }
        
        [Test]
        public async Task When_loading_initial_state_while_resuming_and_an_error_occurs()
        {
            var exception = new Exception(RandomString());
            var start = DateTimeOffset.UtcNow.AddDays(-1);
            var end = DateTimeOffset.UtcNow;
            var resumeTokenJson = RandomString();
            var resumeToken = new ResumeToken
            {
                Cursor = new Cursor(),
                PageEventIndex = 1,
                PageNumber = 2,
                TotalProcessedEvents = 3
            };

            GetMock<IJsonSerializer>().Setup(x => x.Deserialize<ResumeToken>(resumeTokenJson))
                .Returns(resumeToken);
            GetMock<IPager>().Setup(x => x.GetPageAsync(IsAny<Cursor>())).Throws(exception);

            var state = await ClassUnderTest.LoadInitialStateAsync(start, end, resumeTokenJson);

            Assert.That(state.Exception, Is.EqualTo(exception));
            Assert.That(state.Cursor, Is.EqualTo(resumeToken.Cursor));
            Assert.That(state.PreviousCursor, Is.EqualTo(resumeToken.Cursor));
            Assert.That(state.Events, Is.Empty);
            Assert.That(state.PageEventIndex, Is.EqualTo(resumeToken.PageEventIndex));
            Assert.That(state.TotalEventsProcessed, Is.EqualTo(resumeToken.TotalProcessedEvents));
            Assert.That(state.PageNumber, Is.EqualTo(resumeToken.PageNumber));
        }

        [Test]
        public async Task When_loading_next_state()
        {
            var previousState = new WalkState
            {
                PageNumber = 1,
                TotalEventsProcessed = 2,
                Events = new List<WalkedEvent>
                {
                    new WalkedEvent(),
                    new WalkedEvent()
                },
                Cursor = new Cursor(),
                PreviousCursor = new Cursor()
            };
            var page = new Page
            {
                NextPage = new Cursor(),
                Events = new List<WalkedEvent>
                {
                    new WalkedEvent(),
                    new WalkedEvent()
                }
            };

            GetMock<IPager>().Setup(x => x.GetPageAsync(previousState.Cursor))
                .ReturnsAsync(page);

            var state = await ClassUnderTest.LoadNextStateAsync(previousState);

            Assert.That(state.Cursor, Is.EqualTo(page.NextPage));
            Assert.That(state.PreviousCursor, Is.EqualTo(previousState.Cursor));
            Assert.That(state.Events, Is.EqualTo(page.Events));
            Assert.That(state.PageEventIndex, Is.EqualTo(0));
            Assert.That(state.PageNumber, Is.EqualTo(previousState.PageNumber + 1));
            Assert.That(state.TotalEventsProcessed, Is.EqualTo(previousState.TotalEventsProcessed));
        }
        
        [Test]
        public async Task When_loading_next_state_and_the_cursor_is_not_present()
        {
            var previousState = new WalkState
            {
                PageNumber = 1,
                TotalEventsProcessed = 2,
                Events = new List<WalkedEvent>
                {
                    new WalkedEvent(),
                    new WalkedEvent()
                },
                Cursor = null,
                PreviousCursor = new Cursor()
            };
            
            var state = await ClassUnderTest.LoadNextStateAsync(previousState);

            GetMock<IPager>().VerifyNever(x => x.GetPageAsync(IsAny<Cursor>()));

            Assert.That(state.Cursor, Is.Null);
            Assert.That(state.PreviousCursor, Is.EqualTo(previousState.PreviousCursor));
            Assert.That(state.Events, Is.Empty);
            Assert.That(state.PageEventIndex, Is.EqualTo(0));
            Assert.That(state.PageNumber, Is.EqualTo(previousState.PageNumber + 1));
            Assert.That(state.TotalEventsProcessed, Is.EqualTo(previousState.TotalEventsProcessed));
        }

        [Test]
        public async Task When_loading_next_state_and_an_error_occurs()
        {
            var previousState = new WalkState
            {
                PageNumber = 1,
                TotalEventsProcessed = 2,
                Events = new List<WalkedEvent>
                {
                    new WalkedEvent(),
                    new WalkedEvent()
                },
                Cursor = new Cursor(),
                PreviousCursor = new Cursor()
            };
            var exception = new Exception(RandomString());

            GetMock<IPager>().Setup(x => x.GetPageAsync(IsAny<Cursor>()))
                .Throws(exception);

            var state = await ClassUnderTest.LoadNextStateAsync(previousState);

            Assert.That(state.Exception, Is.EqualTo(exception));
            Assert.That(state.Cursor, Is.EqualTo(previousState.Cursor));
            Assert.That(state.PreviousCursor, Is.EqualTo(previousState.PreviousCursor));
            Assert.That(state.Events, Is.EqualTo(previousState.Events));
            Assert.That(state.PageEventIndex, Is.EqualTo(previousState.PageEventIndex));
            Assert.That(state.PageNumber, Is.EqualTo(previousState.PageNumber));
            Assert.That(state.TotalEventsProcessed, Is.EqualTo(previousState.TotalEventsProcessed));
        }
    }
}
