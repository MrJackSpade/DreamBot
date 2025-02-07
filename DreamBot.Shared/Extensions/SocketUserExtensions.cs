using Discord;
using Discord.WebSocket;

namespace DreamBot.Shared.Extensions
{
    public static class SocketUserExtensions
    {
        public static bool HasRole(this IUser user, string role, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
        {
            if (user is SocketGuildUser socketGuildUser)
            {
                return socketGuildUser.HasRole(role);
            }

            return false;
        }
        public static bool HasAnyRole(this SocketGuildUser sgu, params string[] roleNames)
        {
            foreach (string roleName in roleNames)
            {
                if (sgu.HasRole(roleName))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasRole(this SocketGuildUser sgu, string roleName, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
        {
            return sgu.Roles.Any(r => string.Equals(r.Name, roleName, stringComparison));
        }
    }
}