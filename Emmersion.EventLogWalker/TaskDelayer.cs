using System;
using System.Threading.Tasks;

namespace Emmersion.EventLogWalker
{
    internal interface ITaskDelayer
    {
        Task DelayAsync(TimeSpan duration);
    }

    internal class TaskDelayer : ITaskDelayer
    {
        public async Task DelayAsync(TimeSpan duration)
        {
            await Task.Delay(duration);
        }
    }
}
