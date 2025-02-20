﻿using Discord;
using Discord.WebSocket;
using DreamBot.Plugins.EventResults;
using DreamBot.Plugins.Interfaces;
using DreamBot.Plugins.EventArgs;
using DreamBot.Services;
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

            ulong userId = command.TargetUserId;
            int days = command.Days;

            // Get all text channels in the server
            SocketGuild guild = await _discordService.GetGuildAsync(command.Command.GuildId.Value);

			List<ulong> channelIds = ConfigurationService.GetConfiguredChannels().ToList();

			foreach (var channelId in channelIds)
			{
				var channel = guild.GetChannel(channelId);

				if (channel is SocketTextChannel stc)
				{
					Console.WriteLine($"Purging user {userId} from channel {stc.Name}");

					await _discordService.RemoveUserMessagesFromChannel(stc, userId, days);
				}
			}

			return CommandResult.Success("Completed");
        }

        public Task<InitializationResult> OnInitialize(InitializationEventArgs args)
        {
            _discordService = args.DiscordService;
            return InitializationResult.SuccessAsync();
        }
    }
}