using System.Collections.Generic;

namespace Timing.Timers
{
    internal sealed class TimerEntry
    {
        public int id;
        public TimerDomain domain;

        public long dueMs;
        public long intervalMs; // 0 = one-shot
        public bool repeating => intervalMs > 0;

        public bool canceled;
        public bool paused;
        public long remainingMsWhenPaused;

        public string callbackId;

        public string group;
        public List<string> tags; // optional small list
    }
}
