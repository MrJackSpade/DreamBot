using DreamBot.Plugins.EventArgs;

namespace DreamBot.Plugins.Interfaces
{
    public interface IPlugin
    {
        Task OnInitialize(InitializationEventArgs args);
    }
}