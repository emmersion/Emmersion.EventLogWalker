using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Emmersion.EventLogWalker
{
    public interface IPager<TEvent>
        where TEvent : class
    {
        Task<Page<TEvent>> GetPageAsync(Cursor cursor);
    }

    public class Page<TEvent>
    {
        public List<TEvent> Events { get; set; }
        public Cursor NextPage { get; set; }
    }

    public class Cursor
    {
        public DateTimeOffset StartInclusive { get; set; }
        public DateTimeOffset EndExclusive { get; set; }
    }
}
