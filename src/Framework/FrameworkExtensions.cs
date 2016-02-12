using System;

namespace PDS.Framework
{
    public static class FrameworkExtensions
    {
        public static bool EqualsIgnoreCase(this string value, string other)
        {
            return string.Equals(value, other, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
