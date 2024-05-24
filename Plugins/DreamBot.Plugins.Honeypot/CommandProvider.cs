using Discord;
using Discord.WebSocket;
using Dreambot.Plugins.Interfaces;
using DreamBot.Plugins.EventArgs;
using DreamBot.Shared.Interfaces;
using DreamBot.Shared.Models;
using DreamBot.Shared.Utils;
using System.Text;

namespace DreamBot.Plugins.Honeypot
{
    public class CommandProvider : ICommandProvider<ShutdownCommand>
    {
        private const string HONEYPOT_MESSAGE = "Attempted to execute honeypot command";

        private Configuration? _configuration;

        private IDiscordService? _discordService;

        private SocketTextChannel? NotificationChannel;

        public string Command => "Shutdown";

        public string Description => "Disables the bot for all users";

        public SlashCommandOption[] SlashCommandOptions => throw new NotImplementedException();

        public async Task<CommandResult> OnCommand(ShutdownCommand command)
        {
            command = Ensure.NotNull(command);
            ulong guildId = Ensure.NotNull(command.Command?.GuildId);
            ulong userId = Ensure.NotNullOrDefault(command.User?.Id);

            IDiscordService discordService = Ensure.NotNull(_discordService);

            // Get all text channels in the server
            IGuild guild = await discordService.GetGuildAsync(guildId);

            await discordService.BanAsync(guild, userId, HONEYPOT_MESSAGE);

            if (NotificationChannel is not null)
            {
                StringBuilder sb = new();
                sb.AppendLine($"<@{userId}>");
                sb.AppendLine($"`Attempted to execute honeypot command`");
                await NotificationChannel.SendMessageAsync(sb.ToString());
            }

            return CommandResult.Success();
        }

        public async Task OnInitialize(InitializationEventArgs args)
        {
            _configuration = args.LoadConfiguration<Configuration>();

            _discordService = args.DiscordService;

            if (await args.DiscordService.GetChannelAsync(_configuration.NotificationChannelId) is SocketTextChannel stc)
            {
                NotificationChannel = stc;
            }
        }
    }
}