using DreamBot.Plugins.EventArgs;

namespace DreamBot.Shared.Interfaces
{
    public interface IPluginService
    {
        Task PostGenerationEvent(PostGenerationEventArgs postGenerationEventArgs);
		Task PreGenerationEvent(PreGenerationEventArgs postGenerationEventArgs);
	}
}