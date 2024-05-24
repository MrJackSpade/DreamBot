using Discord;
using Discord.WebSocket;
using Dreambot.Plugins.Interfaces;
using DreamBot.Plugins.EventArgs;
using DreamBot.Shared.Interfaces;
using DreamBot.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DreamBot.Plugins.Purge
{
    internal class PurgeCommandProvider : ICommandProvider<PurgeCommand>
    {
        private IDiscordService _discordService;

        public string Command => "Purge";

        public string Description => "Purges all content created by a user";

        public SlashCommandOption[] SlashCommandOptions => [];

        public async Task<CommandResult> OnCommand(PurgeCommand command)
        {
            if (!command.Command.GuildId.HasValue)
            {
                return CommandResult.Error("Not in guild");
            }

            // Get all text channels in the server
            SocketGuild guild = await _discordService.GetGuildAsync(command.Command.GuildId.Value);

            await _discordService.RemoveUserMessages(guild, command.TargetUserId, command.Days);

            return CommandResult.Success("Completed");
        }

        public Task OnInitialize(InitializationEventArgs args)
        {
            _discordService = args.DiscordService;
            return Task.CompletedTask;
        }
    }
}