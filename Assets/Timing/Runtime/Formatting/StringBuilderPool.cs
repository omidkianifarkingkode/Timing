using System.Text;

namespace Timing.Formatting
{
    public static class StringBuilderPool
    {
        [System.ThreadStatic] private static StringBuilder _sb;

        public static StringBuilder Get()
        {
            _sb ??= new StringBuilder(64);
            _sb.Clear();
            return _sb;
        }

        public static string ToStringAndRelease(StringBuilder sb) => sb.ToString();
    }
}
