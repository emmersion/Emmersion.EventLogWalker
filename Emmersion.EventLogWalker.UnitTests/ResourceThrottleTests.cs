using System;
using Emmersion.Testing;
using NUnit.Framework;

namespace Emmersion.EventLogWalker.UnitTests
{
    public class ResourceThrottleTests : With_an_automocked<ResourceThrottle>
    {
        [Test]
        public void When_waiting_for_next_access_and_not_enough_time_has_elapsed()
        {
            ClassUnderTest.MinimumDurationBetweenAccess = TimeSpan.FromSeconds(5);
            ClassUnderTest.LastAccess = DateTimeOffset.UtcNow.AddSeconds(-1);

            TimeSpan? capturedDuration = null;
            GetMock<IThreadSleeper>().Setup(x => x.Sleep(IsAny<TimeSpan>())).Callback<TimeSpan>(duration => capturedDuration = duration);
            
            ClassUnderTest.WaitForNextAccess();
            
            Assert.That(capturedDuration, Is.EqualTo(TimeSpan.FromSeconds(4)).Within(TimeSpan.FromMilliseconds(100)));
        }
        
        [Test]
        public void When_waiting_for_next_access_and_enough_time_has_elapsed()
        {
            ClassUnderTest.MinimumDurationBetweenAccess = TimeSpan.FromSeconds(5);
            ClassUnderTest.LastAccess = DateTimeOffset.UtcNow.AddSeconds(-6);

            ClassUnderTest.WaitForNextAccess();
            
            GetMock<IThreadSleeper>().VerifyNever(x => x.Sleep(IsAny<TimeSpan>()));
        }
    }
}
