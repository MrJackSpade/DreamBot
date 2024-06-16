using Discord;
using DreamBot.Shared.Models;

namespace DreamBot.Plugins.EventArgs
{
    public struct PreGenerationEventArgs
    {
		public IChannel Channel { get; set ; }

		public DateTime DateCreated { get; set; }

		public GenerationParameters GenerationParameters { get; set; }

		public IGuild Guild { get; set; }

		public IUser User { get; set; }
	}
}