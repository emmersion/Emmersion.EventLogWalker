using System;
using System.Threading.Tasks;

namespace Emmersion.EventLogWalker
{
    public interface IResourceThrottle
    {
        Task WaitForNextAccessAsync();
        TimeSpan MinimumDurationBetweenAccess { get; set; }
        DateTimeOffset LastAccess { get; set; }
    }

    public class ResourceThrottle : IResourceThrottle
    {
        private readonly ITaskDelayer taskDelayer;

        public ResourceThrottle(ITaskDelayer taskDelayer)
        {
            this.taskDelayer = taskDelayer;
        }
        
        public TimeSpan MinimumDurationBetweenAccess { get; set; }
        public DateTimeOffset LastAccess { get; set; }

        public async Task WaitForNextAccessAsync()
        {
            var runDuration = DateTimeOffset.UtcNow - LastAccess;
            var remainingDuration = MinimumDurationBetweenAccess - runDuration;
            if (remainingDuration.TotalMilliseconds > 0)
            {
                await taskDelayer.DelayAsync(remainingDuration);
            }
        }
    }
}
