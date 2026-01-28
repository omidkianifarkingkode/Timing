using System;

namespace Timing.Utils
{
    public static class Duration
    {
        public static long Milliseconds(this int v) => v;
        public static long Seconds(this int v) => (long)v * 1000L;
        public static long Minutes(this int v) => (long)v * 60_000L;
        public static long Hours(this int v) => (long)v * 3_600_000L;
        public static long Days(this int v) => (long)v * 86_400_000L;
    }
}
