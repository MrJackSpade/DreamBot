using DreamBot.Plugins.EventResults;
using DreamBot.Plugins.EventArgs;

namespace DreamBot.Plugins.Interfaces
{
    public interface IPlugin
    {
        Task<InitializationResult> OnInitialize(InitializationEventArgs args);
    }
}