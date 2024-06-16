using DreamBot.Plugins.EventArgs;

namespace DreamBot.Plugins.Interfaces
{
    public interface IPreGenerationEventHandler : IPlugin
    {
        Task OnPreGeneration(PreGenerationEventArgs args);
    }
}