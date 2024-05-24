using System.Runtime.CompilerServices;

namespace DreamBot.Shared.Utils
{
    public static class Ensure
    {
        public static T NotNull<T>(T? o, [CallerMemberName] string memberName = "") where T : class
        {
            if (o == null)
            {
                throw new ArgumentNullException(memberName);
            }

            return o;
        }

        public static T NotNull<T>(T? o, [CallerMemberName] string memberName = "") where T : struct
        {
            if (!o.HasValue)
            {
                throw new ArgumentNullException(memberName);
            }

            return o.Value;
        }

        public static ulong NotNullOrDefault(ulong? o, [CallerMemberName] string memberName = "")
        {
            if (!o.HasValue || o.Value == default)
            {
                throw new ArgumentNullException(memberName);
            }

            return o.Value;
        }

        public static T[] NotNullOrEmpty<T>(T[]? o, [CallerMemberName] string memberName = "")
        {
            if (o == null || o.Length == 0)
            {
                throw new ArgumentNullException(memberName);
            }

            return o;
        }

        public static string NotNullOrWhiteSpace(string? o, [CallerMemberName] string memberName = "")
        {
            if (string.IsNullOrWhiteSpace(o))
            {
                throw new ArgumentNullException(memberName);
            }

            return o;
        }
    }
}