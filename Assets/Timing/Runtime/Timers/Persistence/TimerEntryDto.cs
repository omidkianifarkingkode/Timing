using System;
using System.Collections.Generic;

namespace Timing.Timers.Persistence
{
    [Serializable]
    public struct TimerEntryDto
    {
        public int id;
        public TimerDomain domain;
        public long dueMs;
        public long intervalMs;
        public bool paused;
        public long remainingMsWhenPaused;
        public string callbackId;
        public string group;
        public string[] tags;
    }

    [Serializable]
    public struct TimerStateDto
    {
        public TimerEntryDto[] timers;
    }
}
