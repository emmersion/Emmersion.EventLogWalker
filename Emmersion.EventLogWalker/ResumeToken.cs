namespace Emmersion.EventLogWalker
{
    public class ResumeToken
    {
        public Cursor Cursor { get; set; }
        public int PageEventIndex { get; set; }
        public int PageNumber { get; set; }
        public int TotalProcessedEvents { get; set; }
    }
}
