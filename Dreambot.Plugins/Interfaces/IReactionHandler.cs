using DreamBot.Models.Events;
using DreamBot.Plugins.Interfaces;

namespace Dreambot.Plugins.Interfaces
{
    public interface IReactionHandler : IPlugin
    {
        string[] HandledReactions { get; }

        Task OnReaction(ReactionEventArgs args);
    }
}