using Discord.WebSocket;
using DreamBot.Shared.Models;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace DreamBot.Models.Commands
{
    public class CreateThreadCommand : BaseCommand
    {
        public CreateThreadCommand(SocketSlashCommand command) : base(command)
        {
        }

        [IgnoreDataMember]
        [Display(Name = "default_style")]
        public string DefaultStyle { get; set; }

        public string Description { get; set; } = string.Empty;

        [Required]
        public string Title { get; set; }
    }
}