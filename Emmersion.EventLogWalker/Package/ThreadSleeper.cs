using System;
using System.Threading;

namespace Emmersion.EventLogWalker.Package
{
    public interface IThreadSleeper
    {
        void Sleep(TimeSpan duration);
    }

    public class ThreadSleeper : IThreadSleeper
    {
        public void Sleep(TimeSpan duration)
        {
            Thread.Sleep(duration);
        }
    }
}
