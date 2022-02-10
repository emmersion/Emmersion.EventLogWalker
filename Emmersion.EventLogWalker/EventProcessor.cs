using System;
using System.Threading.Tasks;

namespace Emmersion.EventLogWalker
{
    internal interface IEventProcessor<TEvent>
        where TEvent : class
    {
        Task ProcessEventAsync(TEvent walkedEvent, IEventLogWalkerStatus status);
    }

    internal class EventProcessor<TEvent> : IEventProcessor<TEvent>
        where TEvent : class
    {
        private readonly Func<TEvent, IEventLogWalkerStatus, Task> processorFunc;

        public EventProcessor(Func<TEvent, IEventLogWalkerStatus, Task> processorFunc)
        {
            this.processorFunc = processorFunc;
        }

        public EventProcessor(Action<TEvent, IEventLogWalkerStatus> processorFunc)
        {
            this.processorFunc = (insightEvent, status) =>
            {
                processorFunc(insightEvent, status);
                return Task.CompletedTask;
            };
        }

        public Task ProcessEventAsync(TEvent walkedEvent, IEventLogWalkerStatus status)
        {
            return processorFunc(walkedEvent, status);
        }
    }
}
