using Discord;
using Discord.WebSocket;

namespace DreamBot.Models.Commands
{
    public class BaseCommand
    {
        public BaseCommand()
        {
        }

        public IChannel Channel => this.Command.Channel;

        public SocketSlashCommand Command { get; init; }

        public SocketUser? User => this.Command.User;
    }
}