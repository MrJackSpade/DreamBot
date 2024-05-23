using Discord.WebSocket;
using DreamBot.Shared.Models;

namespace DreamBot.Plugins.Honeypot
{
    public class ShutdownCommand : BaseCommand
    {
        public ShutdownCommand(SocketSlashCommand command) : base(command)
        {
        }
    }
}