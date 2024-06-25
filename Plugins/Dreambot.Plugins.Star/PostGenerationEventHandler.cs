using Discord;
using Dreambot.Plugins.EventResults;
using Dreambot.Plugins.Star;
using DreamBot.Plugins.EventArgs;
using DreamBot.Plugins.Interfaces;

namespace DreamBot.Plugins.Star
{
    public class PostGenerationEventHandler : IPostGenerationEventHandler
    {
        public Task<InitializationResult> OnInitialize(InitializationEventArgs args)
        {
            return InitializationResult.SuccessAsync();
        }

        public async Task OnPostGeneration(PostGenerationEventArgs args)
        {
            IEmote starEmote = Emojis.STAR;

            foreach (string prompt_part in args.GenerationParameters.Prompt.Parts.SelectMany(s => s.Split(' ', '_')).Where(s => s.Length > 2).Select(s => s.ToLower()).Distinct())
            {
                if (Emoji.TryParse($":{prompt_part}:", out var emote))
                {
                    starEmote = emote;
                    break;
                }
            }

            await args.Message.AddReactionAsync(starEmote);
        }
    }
}
