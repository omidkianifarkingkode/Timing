using Timing.Clock;
using Timing.Tick;

namespace Timing.Timers
{
    public sealed class RealTimeDomain : ITimeDomain
    {
        private readonly GameClock _clock;
        public TimerDomain Domain => TimerDomain.RealTime;
        public long NowMs => _clock.TrustedUnixMsNow();
        public RealTimeDomain(GameClock clock) => _clock = clock;
    }

    public sealed class AccumulatingDomain : ITimeDomain
    {
        public TimerDomain Domain { get; }
        public long NowMs { get; private set; }

        public AccumulatingDomain(TimerDomain domain) => Domain = domain;

        public void Advance(float dtSeconds)
        {
            if (dtSeconds <= 0f) return;
            NowMs += (long)(dtSeconds * 1000f);
        }
    }
}
