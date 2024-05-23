using Discord;
using DreamBot.Shared.Models;

namespace DreamBot.Plugins.EventArgs
{
    public struct PostGenerationEventArgs
    {
        public readonly IChannel Channel => Message.Channel;

        public DateTime DateCreated { get; set; }

        public GenerationParameters GenerationParameters { get; set; }

        public IGuild Guild { get; set; }

        public GeneratedImage[] Images { get; set; }

        public IMessage Message { get; set; }

        public IUser User { get; set; }
    }
}