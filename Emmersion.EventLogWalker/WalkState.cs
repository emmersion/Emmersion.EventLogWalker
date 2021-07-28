﻿using System;
using System.Collections.Generic;

namespace Emmersion.EventLogWalker
{
    internal class WalkState
    {
        public List<InsightEvent> Events { get; set; } = new List<InsightEvent>();
        public Cursor Cursor { get; set; }
        public Cursor PreviousCursor { get; set; }
        public int PageEventIndex { get; set; }
        public int PageNumber { get; set; }
        public int TotalEventsProcessed { get; set; }
        public Exception Exception { get; set; }
    }
}
