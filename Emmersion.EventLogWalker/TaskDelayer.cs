using System;
using System.Threading.Tasks;

namespace Emmersion.EventLogWalker
{
    public interface ITaskDelayer
    {
        Task DelayAsync(TimeSpan duration);
    }

    public class TaskDelayer : ITaskDelayer
    {
        public async Task DelayAsync(TimeSpan duration)
        {
            await Task.Delay(duration);
        }
    }
}
