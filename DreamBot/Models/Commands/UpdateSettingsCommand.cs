using Discord.WebSocket;
using DreamBot.Shared.Models;

namespace DreamBot.Models.Commands
{
    public class UpdateSettingsCommand : BaseCommand
    {
        public UpdateSettingsCommand(SocketSlashCommand command) : base(command)
        {
        }

        public string DefaultStyle { get; set; }

        public int LandscapeHeight { get; set; }

        public int LandscapeWidth { get; set; }

        public string? NegativePrompt { get; set; }

        public int PortraitHeight { get; set; }

        public int PortraitWidth { get; set; }

        public string? Prompt { get; set; }

        public int SquareHeight { get; set; }

        public int SquareWidth { get; set; }
    }
}