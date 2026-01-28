using System;

namespace Timing.Clock
{
    [Serializable]
    public struct GameClockSnapshot
    {
        public long trustedUnixMsAtSync;
        public long monotonicTicksAtSync;
        public long stopwatchFrequency;
        public long deviceUnixMsAtSync;     // only for tamper diagnostics
        public int tamperCount;
        public long lastKnownTrustedUnixMs; // persisted “last computed” for sanity checks
    }
}
