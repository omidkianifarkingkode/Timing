namespace Timing.Timers
{
    public enum TimerDomain { RealTime, AppTime, GameplayTime }

    public interface ITimeDomain
    {
        TimerDomain Domain { get; }
        long NowMs { get; } // monotonic in-domain "now"
    }
}
