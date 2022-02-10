using System.Threading.Tasks;
using NUnit.Framework;

namespace Emmersion.EventLogWalker.UnitTests
{
    public class EventProcessorTests
    {
        [Test]
        public async Task When_processing_an_event_async()
        {
            var insightEvent = new WalkedEvent();
            var status = new EventLogWalkerStatus(null, null);

            WalkedEvent capturedWalkedEvent = null;
            IEventLogWalkerStatus capturedStatus = null;
            var eventProcessor = new EventProcessor((xEvent, xStatus) =>
            {
                capturedWalkedEvent = xEvent;
                capturedStatus = xStatus;

                return Task.CompletedTask;
            });

            await eventProcessor.ProcessEventAsync(insightEvent, status);

            Assert.That(capturedWalkedEvent, Is.SameAs(insightEvent));
            Assert.That(capturedStatus, Is.SameAs(status));
        }

        [Test]
        public async Task When_processing_an_event_synchronous()
        {
            var insightEvent = new WalkedEvent();
            var status = new EventLogWalkerStatus(null, null);

            WalkedEvent capturedWalkedEvent = null;
            IEventLogWalkerStatus capturedStatus = null;
            var eventProcessor = new EventProcessor((xEvent, xStatus) =>
            {
                capturedWalkedEvent = xEvent;
                capturedStatus = xStatus;
            });

            await eventProcessor.ProcessEventAsync(insightEvent, status);

            Assert.That(capturedWalkedEvent, Is.SameAs(insightEvent));
            Assert.That(capturedStatus, Is.SameAs(status));
        }
    }
}
