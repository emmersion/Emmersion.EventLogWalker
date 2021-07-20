using System;
using System.Threading.Tasks;

namespace Emmersion.EventLogWalker.Package
{
    public interface IEventProcessor
    {
        Task ProcessEventAsync(InsightEvent insightEvent, IEventLogWalkerStatus status);
    }

    public class EventProcessor : IEventProcessor
    {
        private readonly Func<InsightEvent, IEventLogWalkerStatus, Task> processorFunc;

        public EventProcessor(Func<InsightEvent, IEventLogWalkerStatus, Task> processorFunc)
        {
            this.processorFunc = processorFunc;
        }

        public EventProcessor(Action<InsightEvent, IEventLogWalkerStatus> processorFunc)
        {
            this.processorFunc = (insightEvent, status) =>
            {
                processorFunc(insightEvent, status);
                return Task.CompletedTask;
            };
        }

        public Task ProcessEventAsync(InsightEvent insightEvent, IEventLogWalkerStatus status)
        {
            return processorFunc(insightEvent, status);
        }
    }
}
