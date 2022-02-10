using System;
using System.Threading.Tasks;

namespace Emmersion.EventLogWalker
{
    internal interface IEventProcessor
    {
        Task ProcessEventAsync(WalkedEvent walkedEvent, IEventLogWalkerStatus status);
    }

    internal class EventProcessor : IEventProcessor
    {
        private readonly Func<WalkedEvent, IEventLogWalkerStatus, Task> processorFunc;

        public EventProcessor(Func<WalkedEvent, IEventLogWalkerStatus, Task> processorFunc)
        {
            this.processorFunc = processorFunc;
        }

        public EventProcessor(Action<WalkedEvent, IEventLogWalkerStatus> processorFunc)
        {
            this.processorFunc = (insightEvent, status) =>
            {
                processorFunc(insightEvent, status);
                return Task.CompletedTask;
            };
        }

        public Task ProcessEventAsync(WalkedEvent walkedEvent, IEventLogWalkerStatus status)
        {
            return processorFunc(walkedEvent, status);
        }
    }
}
