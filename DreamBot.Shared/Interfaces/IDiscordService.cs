using Discord;
using Discord.WebSocket;

namespace DreamBot.Shared.Interfaces
{
    public interface IDiscordService
    {
        Task BanAsync(IGuild guild, ulong id, string reason);

        ValueTask<IChannel?> GetChannelAsync(ulong channelId);

        ValueTask<SocketGuild> GetGuildAsync(ulong guildId);
    }
}