using DreamBot.Plugins.Interfaces;

namespace Dreambot.Plugins.Interfaces
{
    public interface ICommandProvider : IPlugin
    {
        string Command { get; }

        string Description { get; }
    }

    public interface ICommandProvider<in TCommand> : ICommandProvider
    {
        Task<string> OnCommand(TCommand command);
    }
}