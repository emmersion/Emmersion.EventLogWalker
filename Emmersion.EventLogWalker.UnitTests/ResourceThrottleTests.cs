using System;
using System.Threading.Tasks;
using Emmersion.Testing;
using NUnit.Framework;

namespace Emmersion.EventLogWalker.UnitTests
{
    internal class ResourceThrottleTests : With_an_automocked<ResourceThrottle>
    {
        [Test]
        public async Task When_waiting_for_next_access_and_not_enough_time_has_elapsed()
        {
            ClassUnderTest.MinimumDurationBetweenAccess = TimeSpan.FromSeconds(5);
            ClassUnderTest.LastAccess = DateTimeOffset.UtcNow.AddSeconds(-1);

            TimeSpan? capturedDuration = null;
            GetMock<ITaskDelayer>().Setup(x => x.DelayAsync(IsAny<TimeSpan>())).Callback<TimeSpan>(duration => capturedDuration = duration);
            
            await ClassUnderTest.WaitForNextAccessAsync();
            
            Assert.That(capturedDuration, Is.EqualTo(TimeSpan.FromSeconds(4)).Within(TimeSpan.FromMilliseconds(100)));
        }
        
        [Test]
        public async Task When_waiting_for_next_access_and_enough_time_has_elapsed()
        {
            ClassUnderTest.MinimumDurationBetweenAccess = TimeSpan.FromSeconds(5);
            ClassUnderTest.LastAccess = DateTimeOffset.UtcNow.AddSeconds(-6);

            await ClassUnderTest.WaitForNextAccessAsync();
            
            GetMock<ITaskDelayer>().VerifyNever(x => x.DelayAsync(IsAny<TimeSpan>()));
        }
    }
}
