using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Emmersion.EventLogWalker;

namespace ExampleReports
{
    public interface ISimpleReport
    {
        Task GenerateAsync();
    }

    public class SimpleReport : ISimpleReport
    {
        private readonly IEventLogWalker walker;

        public SimpleReport(IEventLogWalker walker)
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
