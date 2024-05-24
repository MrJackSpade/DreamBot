using Discord.WebSocket;
using DreamBot.Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace DreamBot.Plugins.Purge
{
    public class PurgeCommand : BaseCommand
    {
        public PurgeCommand(SocketSlashCommand command) : base(command)
        {
        }

        [Required]
        public int Days { get; set; }

        [Required]
        public ulong TargetUserId { get; set; }
    }
}