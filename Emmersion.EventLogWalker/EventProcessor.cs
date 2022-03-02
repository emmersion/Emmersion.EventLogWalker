using System;
using System.Threading.Tasks;

namespace Emmersion.EventLogWalker
{
    internal interface IEventProcessor<TEvent>
        where TEvent : class
    {
        Task ProcessEventAsync(TEvent @event, IEventLogWalkerStatus status);
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
            this.processorFunc = (@event, status) =>
            {
                processorFunc(@event, status);
                return Task.CompletedTask;
            };
        }

        public Task ProcessEventAsync(TEvent @event, IEventLogWalkerStatus status)
        {
            return processorFunc(@event, status);
        }
    }
}
