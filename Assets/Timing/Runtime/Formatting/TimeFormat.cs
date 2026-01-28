using System;
using System.Text;

namespace Timing.Formatting
{
    public static class TimeFormat
    {
        public static string HHmmss(long totalSeconds)
        {
            var sb = StringBuilderPool.Get();
            var h = totalSeconds / 3600;
            var m = (totalSeconds % 3600) / 60;
            var s = totalSeconds % 60;

            Append2(sb, (int)h); sb.Append(':');
            Append2(sb, (int)m); sb.Append(':');
            Append2(sb, (int)s);

            return StringBuilderPool.ToStringAndRelease(sb);
        }

        public static string MMss(long totalSeconds)
        {
            var sb = StringBuilderPool.Get();
            var m = totalSeconds / 60;
            var s = totalSeconds % 60;

            Append2(sb, (int)m); sb.Append(':');
            Append2(sb, (int)s);

            return StringBuilderPool.ToStringAndRelease(sb);
        }

        public static string Compact(long totalSeconds)
        {
            var sb = StringBuilderPool.Get();

            var days = totalSeconds / 86400; totalSeconds %= 86400;
            var hours = totalSeconds / 3600; totalSeconds %= 3600;
            var mins = totalSeconds / 60;
            var secs = totalSeconds % 60;

            if (days > 0) { sb.Append(days).Append("d "); }
            if (hours > 0) { sb.Append(hours).Append("h "); }
            if (mins > 0) { sb.Append(mins).Append("m "); }
            if (secs > 0 || sb.Length == 0) { sb.Append(secs).Append("s"); }

            return StringBuilderPool.ToStringAndRelease(sb);
        }

        public static string LargestUnit(long totalSeconds)
        {
            if (totalSeconds >= 86400) return (totalSeconds / 86400) + "d";
            if (totalSeconds >= 3600) return (totalSeconds / 3600) + "h";
            if (totalSeconds >= 60) return (totalSeconds / 60) + "m";
            return totalSeconds + "s";
        }

        private static void Append2(StringBuilder sb, int v)
        {
            if (v < 10) sb.Append('0');
            sb.Append(v);
        }
    }
}
