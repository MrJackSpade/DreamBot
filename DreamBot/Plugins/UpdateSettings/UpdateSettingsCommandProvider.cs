using DreamBot.Models;
using DreamBot.Plugins.EventArgs;
using DreamBot.Plugins.EventResults;
using DreamBot.Plugins.Interfaces;
using DreamBot.Services;
using DreamBot.Shared.Models;
using Newtonsoft.Json;

namespace DreamBot.Plugins.UpdateSettings
{
    internal class UpdateSettingsCommandProvider : ICommandProvider<UpdateSettingsCommand>
    {
        public string Command => "Settings";

        public string Description => "Updates Settings";

        public SlashCommandOption[] SlashCommandOptions => [];

        public Task<CommandResult> OnCommand(UpdateSettingsCommand command)
        {
            if (command.Channel is null || command.User is null)
            {
                return CommandResult.ErrorAsync("Invalid Request");
            }

            ChannelConfiguration channelConfiguration = ConfigurationService.GetChannelConfiguration(command.Channel.Id);

            if (command.LandscapeWidth != 0)
            {
                channelConfiguration.Resolutions[nameof(AspectRatio.Landscape)].Width = command.LandscapeWidth;
            }

            if (command.LandscapeHeight != 0)
            {
                channelConfiguration.Resolutions[nameof(AspectRatio.Landscape)].Height = command.LandscapeHeight;
            }

            if (command.PortraitWidth != 0)
            {
                channelConfiguration.Resolutions[nameof(AspectRatio.Portrait)].Width = command.PortraitWidth;
            }

            if (command.PortraitHeight != 0)
            {
                channelConfiguration.Resolutions[nameof(AspectRatio.Portrait)].Height = command.PortraitHeight;
            }

            if (command.SquareWidth != 0)
            {
                channelConfiguration.Resolutions[nameof(AspectRatio.Square)].Width = command.SquareWidth;
            }

            if (command.SquareHeight != 0)
            {
                channelConfiguration.Resolutions[nameof(AspectRatio.Square)].Height = command.SquareHeight;
            }

            if (command.Prompt != null)
            {
                channelConfiguration.Prompt = command.Prompt;
            }

            if (command.NegativePrompt != null)
            {
                channelConfiguration.NegativePrompt = command.NegativePrompt;
            }

            if (!string.IsNullOrWhiteSpace(command.DefaultStyle))
            {
                channelConfiguration.DefaultStyle = command.DefaultStyle;
            }

            if (command.SamplerIndex is not null)
            {
                channelConfiguration.SamplerIndex = command.SamplerIndex;
            }

            if (!string.IsNullOrWhiteSpace(command.SamplerName))
            {
                channelConfiguration.SamplerName = command.SamplerName;
            }

            if (command.CfgScale.HasValue)
            {
                channelConfiguration.CfgScale = command.CfgScale.Value;
            }

            if (command.Steps != 0)
            {
                channelConfiguration.Steps = command.Steps;
            }

            ConfigurationService.SaveChannelConfiguration(command.Channel.Id, channelConfiguration);

            string settingValue = JsonConvert.SerializeObject(channelConfiguration, Formatting.Indented);

            return CommandResult.SuccessAsync($"```{settingValue}```");
        }

        public Task<InitializationResult> OnInitialize(InitializationEventArgs args)
        {
            return InitializationResult.SuccessAsync();
        }
    }
}