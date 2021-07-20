﻿using System.Threading.Tasks;
using Emmersion.EventLogWalker.Package;
using NUnit.Framework;

namespace Emmersion.EventLogWalker.Tests.Package
{
    public class EventProcessorTests
    {
        [Test]
        public async Task When_processing_an_event_async()
        {
            var insightEvent = new InsightEvent();
            var status = new EventLogWalkerStatus(null, null);

            InsightEvent capturedInsightEvent = null;
            IEventLogWalkerStatus capturedStatus = null;
            var eventProcessor = new EventProcessor((xEvent, xStatus) =>
            {
                capturedInsightEvent = xEvent;
                capturedStatus = xStatus;

                return Task.CompletedTask;
            });

            await eventProcessor.ProcessEventAsync(insightEvent, status);

            Assert.That(capturedInsightEvent, Is.SameAs(insightEvent));
            Assert.That(capturedStatus, Is.SameAs(status));
        }

        [Test]
        public async Task When_processing_an_event_synchronous()
        {
            var insightEvent = new InsightEvent();
            var status = new EventLogWalkerStatus(null, null);

            InsightEvent capturedInsightEvent = null;
            IEventLogWalkerStatus capturedStatus = null;
            var eventProcessor = new EventProcessor((xEvent, xStatus) =>
            {
                capturedInsightEvent = xEvent;
                capturedStatus = xStatus;
            });

            await eventProcessor.ProcessEventAsync(insightEvent, status);

            Assert.That(capturedInsightEvent, Is.SameAs(insightEvent));
            Assert.That(capturedStatus, Is.SameAs(status));
        }
    }
}
