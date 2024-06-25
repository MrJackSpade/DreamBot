using Dreambot.Plugins.EventResults;
using DreamBot.Constants;
using DreamBot.Plugins.EventArgs;
using DreamBot.Plugins.Interfaces;

namespace DreamBot.Plugins.Lolice
{
    public class PostGenerationEventHandler : IPostGenerationEventHandler
    {
        public Task<InitializationResult> OnInitialize(InitializationEventArgs args)
        {
            return InitializationResult.SuccessAsync();
        }

        public async Task OnPostGeneration(PostGenerationEventArgs args)
        {
            await args.Message.AddReactionAsync(Emojis.LOLICE);
        }
    }
}
