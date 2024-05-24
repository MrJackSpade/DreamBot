using DreamBot.Plugins.EventArgs;

namespace DreamBot.Shared.Interfaces
{
    public interface IPluginService
    {
        void PostGenerationEvent(PostGenerationEventArgs postGenerationEventArgs);
    }
}