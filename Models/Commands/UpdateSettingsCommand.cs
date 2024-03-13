namespace DreamBot.Models.Commands
{
    public class UpdateSettingsCommand : BaseCommand
    {
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