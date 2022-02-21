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
        private readonly IPager<InsightEvent> pager;

        public SimpleReport(IEventLogWalker walker, IPager<InsightEvent> pager)
        {
            this.walker = walker;
            this.pager = pager;
        }

        public async Task GenerateAsync()
        {
            Console.WriteLine("WARNING: This report runs across the entire event log. This report is for example only. Recommend abort after a few events are processed.");

            var eventCounts = new Dictionary<string, int>();

            var i = 0;
            var finalStatus = await walker.WalkAsync(new WalkArgs<InsightEvent>{ Pager = pager }, (insightEvent, status) =>
            {
                i++;
                if (i % 1000 == 0)
                {
                    Console.WriteLine($"Processed {i} events");
                }

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
