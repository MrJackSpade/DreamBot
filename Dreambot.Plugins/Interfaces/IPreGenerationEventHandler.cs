using DreamBot.Plugins.EventArgs;

namespace DreamBot.Plugins.Interfaces
{
    public interface IPreGenerationEventHandler : IPlugin
    {
        void OnPreGeneration(PostGenerationEventArgs args);
    }
}