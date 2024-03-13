using Discord.WebSocket;

namespace DreamBot.Models.Commands
{
    public class BaseCommand
    {
        public BaseCommand()
        {
        }

        public ulong? ChannelId { get; set; }

        public SocketUser? User { get; set; }
    }
}