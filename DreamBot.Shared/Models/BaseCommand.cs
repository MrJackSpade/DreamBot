using Discord;
using Discord.WebSocket;

namespace DreamBot.Shared.Models
{
    public class BaseCommand
    {
        public BaseCommand(SocketSlashCommand command)
        {
            Command = command;
        }

        public IChannel Channel => this.Command.Channel;

        public SocketSlashCommand Command { get; }

        public SocketUser? User => this.Command.User;
    }
}