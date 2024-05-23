using DreamBot.Plugins.EventArgs;

namespace DreamBot.Plugins.Interfaces
{
    public interface IPostGenerationEventHandler : IPlugin
    {
        Task OnPostGeneration(PostGenerationEventArgs args);
    }
}