using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Emmersion.EventLogWalker
{
    public interface IPager
    {
        Task<Page> GetPageAsync(Cursor cursor);
    }

    public class Page
    {
        public List<WalkedEvent> Events { get; set; }
        public Cursor NextPage { get; set; }
    }

    public class WalkedEvent
    {
        public object Event { get; set; }
    }

    public class Cursor
    {
        public DateTimeOffset StartInclusive { get; set; }
        public DateTimeOffset EndExclusive { get; set; }
    }
}
