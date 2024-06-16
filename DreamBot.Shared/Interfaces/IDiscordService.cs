using Discord;
using Discord.WebSocket;

namespace DreamBot.Shared.Interfaces
{
	public interface IDiscordService
	{
		IUser User { get; }

		Task BanAsync(IGuild guild, ulong id, string reason);

		ValueTask<IChannel?> GetChannelAsync(ulong channelId);

		ValueTask<SocketGuild> GetGuildAsync(ulong guildId);

		Task RemoveUserMessages(SocketGuild guild, ulong targetUserId, int days);

		Task RemoveUserMessagesFromChannel(SocketTextChannel stc, ulong userId, int days = 7);
	}
}