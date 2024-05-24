using DreamBot.Constants;
using DreamBot.Plugins.EventArgs;
using DreamBot.Plugins.Interfaces;

namespace DreamBot.Plugins.Lolice
{
    public class PostGenerationEventHandler : IPostGenerationEventHandler
    {
        public Task OnInitialize(InitializationEventArgs args)
        {
            return Task.CompletedTask;
        }

        public async Task OnPostGeneration(PostGenerationEventArgs args)
        {
            await args.Message.AddReactionAsync(Emojis.LOLICE);
        }
    }
}
