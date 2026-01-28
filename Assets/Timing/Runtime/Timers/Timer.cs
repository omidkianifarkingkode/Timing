using System;
using Timing.Utils;

namespace Timing.Timers
{
    public readonly struct TimerHandle
    {
        public readonly int Id;
        internal TimerHandle(int id) => Id = id;
        public bool IsValid => Id != 0;
    }

    public static class Timer
    {
        // Set once on bootstrap
        public static TimerScheduler Scheduler { get; private set; }
        public static TimerCallbackRegistry Registry { get; private set; }

        public static void Initialize(TimerScheduler scheduler, TimerCallbackRegistry registry)
        {
            Scheduler = scheduler;
            Registry = registry;
        }

        public static TimerHandle After(long delayMs, Action cb, TimerDomain domain, string group = null, params string[] tags)
        {
            var id = CallbackId(cb);
            Registry.Register(id, cb);
            return Scheduler.After(delayMs, id, domain, group, tags);
        }

        public static TimerHandle Every(long intervalMs, Action cb, TimerDomain domain, string group = null, params string[] tags)
        {
            var id = CallbackId(cb);
            Registry.Register(id, cb);
            return Scheduler.Every(intervalMs, id, domain, group, tags);
        }

        public static TimerHandle At(DateTimeOffset utcTime, Action cb, string group = null, params string[] tags)
        {
            var id = CallbackId(cb);
            Registry.Register(id, cb);
            return Scheduler.AtUnixMs(utcTime.ToUnixTimeMilliseconds(), id, group, tags);
        }

        // Simple callback id strategy:
        // For production, prefer explicit string ids you control.
        private static string CallbackId(Action cb) => cb.Method.DeclaringType.FullName + "." + cb.Method.Name;
    }
}
