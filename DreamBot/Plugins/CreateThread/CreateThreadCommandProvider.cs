using Discord.Rest;
using Discord.WebSocket;
using Dreambot.Plugins.Interfaces;
using DreamBot.Models;
using DreamBot.Plugins.EventArgs;
using DreamBot.Services;
using DreamBot.Shared.Models;
using DreamBot.Shared.Utils;

namespace DreamBot.Plugins.NewThread
{
    internal class CreateThreadCommandProvider : ICommandProvider<CreateThreadCommand>
    {
        private Configuration? _configuration;

        private string[]? _styles;

        public string Command => "new_thread";

        public string Description => "Creates a new thread for image generation";

        public SlashCommandOption[] SlashCommandOptions { get; set; } = Array.Empty<SlashCommandOption>();

        public async Task<CommandResult> OnCommand(CreateThreadCommand command)
        {
            Configuration configuration = Ensure.NotNull(_configuration);
            string[] styles = Ensure.NotNullOrEmpty(_styles);
            ulong userId = Ensure.NotNull(command.User).Id;
            string defaultStyle = Ensure.NotNullOrWhiteSpace(command.DefaultStyle);

            if (command.Channel == null)
            {
                return CommandResult.Error("Null channel");
            }

            ulong channelid = command.Channel.Id;

            if (configuration.ThreadCreationChannels is null)
            {
                return CommandResult.Error("Configuration contains null value for ThreadCreationChannels");
            }

            if (!configuration.ThreadCreationChannels.Contains(channelid))
            {
                return CommandResult.Error("Can not create a thread in this channel");
            }

            if (command.Channel is not SocketThreadChannel stc)
            {
                return CommandResult.Error("Current channel is not SocketThreadChannel");
            }

            if (stc.ParentChannel is not SocketForumChannel sfc)
            {
                return CommandResult.Error("Parent channel is not SocketForumChannel");
            }

            if (configuration.Styles.FirstOrDefault(s => s.DisplayName == command.DefaultStyle) is not Style dStyle)
            {
                return CommandResult.Error($"No configured style with name {defaultStyle} found");
            }

            string desc = command.Description;

            if (string.IsNullOrWhiteSpace(desc))
            {
                desc = command.Title;
            }

            desc = $"{desc} by <@{userId}>";

            RestThreadChannel ric = await sfc.CreatePostAsync(command.Title, text: desc);

            ChannelConfiguration channelConfiguration = ConfigurationService.GetChannelConfiguration(ric.Id);

            channelConfiguration.DefaultStyle = command.DefaultStyle;

            channelConfiguration.Prompt = Ensure.NotNullOrWhiteSpace(dStyle.PositivePrompt);
            channelConfiguration.NegativePrompt = Ensure.NotNullOrWhiteSpace(dStyle.NegativePrompt);

            ConfigurationService.SaveChannelConfiguration(ric.Id, channelConfiguration);

            string message = $"Your thread was created > https://discord.com/channels/{channelid}/{ric.Id}";

            await foreach (IReadOnlyCollection<RestMessage>? messages in ric.GetMessagesAsync(100))
            {
                if (messages.FirstOrDefault() is RestUserMessage rum)
                {
                    await rum.PinAsync();
                    return CommandResult.Success(message);
                }
            }

            return CommandResult.Success(message);
        }

        public Task OnInitialize(InitializationEventArgs args)
        {
            _configuration = args.LoadConfiguration<Configuration>();

            _styles = (_configuration?.Styles?.Select(m => m.DisplayName)?.ToArray() ?? [])!;

            if (_styles.Length < 1)
            {
                _styles = ["Default"];
            }

            this.SlashCommandOptions = [new("default_style", "The type of model to use for image generation", true, _styles)];

            return Task.CompletedTask;
        }
    }
}