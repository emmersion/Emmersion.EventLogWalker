using System;

namespace Emmersion.EventLogWalker
{
    public interface IResourceThrottle
    {
        void WaitForNextAccess();
        TimeSpan MinimumDurationBetweenAccess { get; set; }
        DateTimeOffset LastAccess { get; set; }
    }

    public class ResourceThrottle : IResourceThrottle
    {
        private readonly IThreadSleeper threadSleeper;

        public ResourceThrottle(IThreadSleeper threadSleeper)
        {
            this.threadSleeper = threadSleeper;
        }
        
        public TimeSpan MinimumDurationBetweenAccess { get; set; }
        public DateTimeOffset LastAccess { get; set; }

        public void WaitForNextAccess()
        {
            var runDuration = DateTimeOffset.UtcNow - LastAccess;
            var remainingDuration = MinimumDurationBetweenAccess - runDuration;
            if (remainingDuration.TotalMilliseconds > 0)
            {
                threadSleeper.Sleep(remainingDuration);
            }
        }
    }
}
