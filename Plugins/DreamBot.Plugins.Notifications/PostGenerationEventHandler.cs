using Discord;
using Discord.WebSocket;
using Dreambot.Plugins.EventResults;
using DreamBot.Plugins.EventArgs;
using DreamBot.Plugins.Interfaces;
using DreamBot.Shared.Exceptions;
using DreamBot.Shared.Extensions;
using System.Text;
using System.Text.RegularExpressions;

namespace DreamBot.Plugins.Notifications
{
    public class PostGenerationEventHandler : IPostGenerationEventHandler
    {
        private Configuration? _configuration;

        private SocketTextChannel? NotificationChannel;

        public async Task<InitializationResult> OnInitialize(InitializationEventArgs args)
        {
            _configuration = args.LoadConfiguration<Configuration>();

            if(_configuration.NotificationChannelId == 0)
            {
                return InitializationResult.Cancel();
            }

            if (await args.DiscordService.GetChannelAsync(_configuration.NotificationChannelId) is SocketTextChannel stc)
            {
                NotificationChannel = stc;
            }
            else
            {
                throw new MissingChannelException(_configuration.NotificationChannelId);
            }

            return InitializationResult.Success();
        }

        public async Task OnPostGeneration(PostGenerationEventArgs args)
        {
            if (args.User is SocketGuildUser sgu)
            {
                if (sgu.HasAnyRole("mod", "contributor"))
                {
                    return;
                }
            }

            if (args.Guild is not SocketGuild g)
            {
                return;
            }

            if (_configuration.NotificationChannelId != 0 && _configuration.NotificationTriggers.Length > 0)
            {
                IChannel c = args.Channel;

                foreach (string trigger in _configuration.NotificationTriggers)
                {
                    bool isMatch = false;

                    foreach (string promptPart in args.GenerationParameters.Prompt.Parts)
                    {
                        isMatch = isMatch || Regex.IsMatch(promptPart, trigger, RegexOptions.IgnoreCase);
                    }

                    if (isMatch)
                    {
                        StringBuilder sb = new();
                        sb.AppendLine($"https://discord.com/channels/{g.Id}/{c.Id}/{args.Message.Id}");
                        sb.AppendLine($"<@{args.User.Id}>");
                        sb.AppendLine($"`{string.Join(", ", args.GenerationParameters.Prompt.Text)}`");
                        await NotificationChannel?.SendMessageAsync(sb.ToString());
                    }
                }
            }
        }
    }
}