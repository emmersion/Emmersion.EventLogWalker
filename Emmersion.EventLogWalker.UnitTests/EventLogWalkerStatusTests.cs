using System;
using System.Collections.Generic;
using Emmersion.Testing;
using NUnit.Framework;

namespace Emmersion.EventLogWalker.UnitTests
{
    internal class EventLogWalkerStatusTests : With_an_automocked<EventLogWalkerStatus>
    {
        [Test]
        public void When_getting_current_event_index()
        {
            var walkState = new WalkState
            {
                PageEventIndex = 1
            };

            var status = new EventLogWalkerStatus(walkState, null);
            Assert.That(status.PageEventIndex, Is.EqualTo(walkState.PageEventIndex));
        }

        [Test]
        public void When_getting_count_of_events_in_page()
        {
            var walkState = new WalkState
            {
                Events = new List<WalkedEvent>
                {
                    new WalkedEvent(),
                    new WalkedEvent()
                }
            };

            var status = new EventLogWalkerStatus(walkState, null);
            Assert.That(status.PageEventsCount, Is.EqualTo(walkState.Events.Count));
        }

        [Test]
        public void When_getting_page_number()
        {
            var walkState = new WalkState
            {
                PageNumber = 1
            };

            var status = new EventLogWalkerStatus(walkState, null);
            Assert.That(status.PageNumber, Is.EqualTo(walkState.PageNumber));
        }

        [Test]
        public void When_the_first_event_of_the_page_is_being_processed()
        {
            var walkState = new WalkState
            {
                PageEventIndex = 0,
                Events = new List<WalkedEvent>
                {
                    new WalkedEvent(),
                    new WalkedEvent()
                }
            };

            var status = new EventLogWalkerStatus(walkState, null);
            Assert.That(status.PageStatus, Is.EqualTo(PageStatus.Start));
        }

        [Test]
        public void When_the_last_event_of_the_page_is_being_processed()
        {
            var walkState = new WalkState
            {
                PageEventIndex = 2,
                Events = new List<WalkedEvent>
                {
                    new WalkedEvent(),
                    new WalkedEvent(),
                    new WalkedEvent()
                }
            };

            var status = new EventLogWalkerStatus(walkState, null);
            Assert.That(status.PageStatus, Is.EqualTo(PageStatus.End));
        }

        [Test]
        public void When_all_events_for_the_page_have_been_processed()
        {
            var walkState = new WalkState
            {
                PageEventIndex = 3,
                Events = new List<WalkedEvent>
                {
                    new WalkedEvent(),
                    new WalkedEvent(),
                    new WalkedEvent()
                }
            };

            var status = new EventLogWalkerStatus(walkState, null);
            Assert.That(status.PageStatus, Is.EqualTo(PageStatus.Done));
        }

        [Test]
        public void When_an_event_of_the_page_is_being_processed_which_is_not_the_first_or_last()
        {
            var walkState = new WalkState
            {
                PageEventIndex = 1,
                Events = new List<WalkedEvent>
                {
                    new WalkedEvent(),
                    new WalkedEvent(),
                    new WalkedEvent()
                }
            };

            var status = new EventLogWalkerStatus(walkState, null);
            Assert.That(status.PageStatus, Is.EqualTo(PageStatus.InProgress));
        }

        [Test]
        public void When_the_page_is_empty()
        {
            var walkState = new WalkState
            {
                PageEventIndex = 0,
                Events = new List<WalkedEvent>()
            };

            var status = new EventLogWalkerStatus(walkState, null);
            Assert.That(status.PageStatus, Is.EqualTo(PageStatus.Empty));
        }

        [Test]
        public void When_getting_total_processed_events()
        {
            var walkState = new WalkState
            {
                TotalEventsProcessed = 5
            };

            var status = new EventLogWalkerStatus(walkState, null);
            Assert.That(status.TotalEventsProcessed, Is.EqualTo(walkState.TotalEventsProcessed));
        }

        [Test]
        public void When_getting_the_resume_token()
        {
            var walkState = new WalkState
            {
                Cursor = new Cursor(),
                PreviousCursor = new Cursor(),
                PageEventIndex = 1,
                PageNumber = 2,
                TotalEventsProcessed = 3
            };
            var expectedTokenJson = RandomString();
            ResumeToken capturedToken = null;

            var jsonSerializerMock = GetMock<IJsonSerializer>();
            jsonSerializerMock.Setup(x => x.Serialize(IsAny<ResumeToken>()))
                .Callback<ResumeToken>(token => capturedToken = token)
                .Returns(expectedTokenJson);

            var status = new EventLogWalkerStatus(walkState, jsonSerializerMock.Object);
            var tokenJson = status.GetResumeToken();

            Assert.That(tokenJson, Is.EqualTo(expectedTokenJson));
            Assert.That(capturedToken, Is.Not.Null);
            Assert.That(capturedToken.Cursor, Is.EqualTo(walkState.PreviousCursor));
            Assert.That(capturedToken.PageEventIndex, Is.EqualTo(walkState.PageEventIndex));
            Assert.That(capturedToken.PageNumber, Is.EqualTo(walkState.PageNumber));
            Assert.That(capturedToken.TotalProcessedEvents, Is.EqualTo(walkState.TotalEventsProcessed));
        }

        [Test]
        public void When_getting_the_error_message()
        {
            var walkState = new WalkState
            {
                Exception = new Exception(RandomString())
            };

            var status = new EventLogWalkerStatus(walkState, null);
            Assert.That(status.Exception, Is.EqualTo(walkState.Exception));
        }
    }
}
