using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Emmersion.EventLogWalker.Package;

namespace Emmersion.EventLogWalker.Consumer
{
    public interface ISimplestReport
    {
        Task GenerateAsync();
    }

    public class SimplestReport : ISimplestReport
    {
        private readonly IEventLogWalker walker;

        public SimplestReport(IEventLogWalker walker)
        {
            this.walker = walker;
        }

        public async Task GenerateAsync()
        {
            var eventCounts = new Dictionary<string, int>();
            
            var finalStatus = await walker.WalkAsync(new WalkArgs(), (insightEvent, status) =>
            {
                if (eventCounts.ContainsKey(insightEvent.EventType))
                {
                    eventCounts[insightEvent.EventType] += 1;
                }
                else
                {
                    eventCounts[insightEvent.EventType] = 1;
                }
            });

            if (finalStatus.Exception != null)
            {
                Console.WriteLine(finalStatus.Exception);
                return;
            }
            
            foreach (var eventKey in eventCounts.Keys)
            {
                Console.WriteLine($"{eventKey},{eventCounts[eventKey]}");
            }
        }
    }
}
