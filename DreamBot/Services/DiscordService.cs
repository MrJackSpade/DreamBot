using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DreamBot.Attributes;
using DreamBot.Exceptions;
using DreamBot.Extensions;
using DreamBot.Models;
using DreamBot.Models.Automatic;
using DreamBot.Models.Events;
using DreamBot.Shared.Interfaces;
using DreamBot.Shared.Models;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace DreamBot.Services
{
    public class DiscordService : IDiscordService
    {
        public EventHandler<TextToImageProgress> ProgressUpdated;

        public Func<ReactionEventArgs, Task> ReactionAdded;

        private readonly Dictionary<string, Func<SocketSlashCommand, Task<string>>> _commandCallbacks = [];

        private readonly DiscordSocketClient _discordClient;

        private readonly TaskCompletionSource _readyTask = new();

        private readonly DiscordServiceSettings _settings;

        public DiscordService(DiscordServiceSettings settings)
        {
            this._settings = settings;
            this._discordClient = new DiscordSocketClient();
            this._discordClient.Log += this.DiscordClient_Log;
            this._discordClient.Ready += this.DiscordClient_Ready;
            this._discordClient.SlashCommandExecuted += this.SlashCommandHandler;
            this._discordClient.ReactionAdded += async (cache, channel, reaction) =>
            {
                if (this.ReactionAdded != null)
                {
                    await this.ReactionAdded.Invoke(new ReactionEventArgs(cache, channel, reaction));
                }
            };
        }

        public IUser User { get; set; }

        public async Task AddCommand(string command, string description, Type t, Func<BaseCommand, Task<string>> action, params SlashCommandOption[] slashCommandOptions)
        {
            command = command.ToLower();

            this._commandCallbacks.Add(command, (c) => action.Invoke(CastType(c, t)));

            await this.AddCommand(command, description, t, slashCommandOptions);
        }

        public async Task BanAsync(IGuild guild, ulong id, string reason)
        {
            await guild.AddBanAsync(id, reason: reason);
        }

        public async Task Connect()
        {
            await this._discordClient.LoginAsync(TokenType.Bot, this._settings.Token);
            await this._discordClient.StartAsync();
            await this._readyTask.Task;
            this.User = this._discordClient.CurrentUser;
        }

        public async Task<GenerationPlaceholder> CreateMessage(string title, ulong channel, AllowedMentions? allowedMentions = null, MessageFlags flags = MessageFlags.None)
        {
            IChannel socketChannel = await this._discordClient.GetChannelAsync(channel);

            return await this.CreateMessage(title, socketChannel, allowedMentions, flags);
        }

        public async Task<GenerationPlaceholder> CreateMessage(string title, IChannel socketChannel, AllowedMentions? allowedMentions = null, MessageFlags flags = MessageFlags.None)
        {
            if (socketChannel is SocketTextChannel stc)
            {
                RestUserMessage message = await stc.SendMessageAsync(title, allowedMentions: allowedMentions, flags: flags);

                return new GenerationPlaceholder(message);
            }

            throw new Exception();
        }

        public ValueTask<IChannel?> GetChannelAsync(ulong channelId)
        {
            return _discordClient.GetChannelAsync(channelId);
        }

        public ValueTask<SocketGuild> GetGuildAsync(ulong guildId)
        {
            return ValueTask.FromResult(_discordClient.GetGuild(guildId));
        }

        public async Task RemoveUserMessages(SocketGuild guild, ulong userId, int days = 7)
        {
            // Get all text channels in the server
            var channels = guild.TextChannels;

            foreach (var channel in channels)
            {
                Console.WriteLine($"Purging user {userId} from channel {channel.Name}");
                await this.RemoveUserMessagesFromChannel(channel, userId, days);
            }

            Console.WriteLine($"Purging user {userId} completed");
        }

        public async Task RemoveUserMessages(SocketGuild guild, IUser user, int days = 7)
        {
            await this.RemoveUserMessages(guild, user.Id, days);
        }

        private static BaseCommand CastType(SocketSlashCommand source, Type t)
        {
            if (!typeof(BaseCommand).IsAssignableFrom(t))
            {
                throw new ArgumentException("Cast type must inherit from base command");
            }

            BaseCommand payload = (BaseCommand)Activator.CreateInstance(t, [source])!;

            Dictionary<string, PropertyInfo> propertyDict = [];

            foreach (PropertyInfo pi in t.GetProperties())
            {
                string name = pi.Name.ToLower();

                if (pi.GetCustomAttribute<DisplayAttribute>() is DisplayAttribute d)
                {
                    if (!string.IsNullOrWhiteSpace(d.Name))
                    {
                        name = d.Name.ToLower();
                    }
                }

                propertyDict.Add(name, pi);
            }

            foreach (SocketSlashCommandDataOption? option in source.Data.Options)
            {
                try
                {
                    if (propertyDict.TryGetValue(option.Name, out PropertyInfo prop))
                    {
                        if (prop.PropertyType == typeof(List<string>))
                        {
                            bool isDistinct = prop.GetCustomAttribute<DistinctAttribute>() is not null;

                            string v = option.Value.ToString();

                            List<string> values = [];

                            if (!string.IsNullOrWhiteSpace(v))
                            {
                                foreach (string part in v.Split(',', ';').Select(l => l.Trim()))
                                {
                                    values.Add(part);
                                }

                                if (isDistinct)
                                {
                                    values = values.Distinct().ToList();
                                }

                                prop.SetValue(payload, values);
                            }
                        }
                        else if (prop.PropertyType == typeof(bool))
                        {
                            string b = option.Value.ToString().ToLower();

                            prop.SetValue(payload, b != "false");
                        }
                        else if (prop.PropertyType.IsEnum)
                        {
                            if (!Enum.TryParse(prop.PropertyType, option.Value.ToString(), out object value))
                            {
                                throw new CommandPropertyValidationException(option.Name, $"'{option.Value}' is not a valid value");
                            }
                            else
                            {
                                prop.SetValue(payload, value);
                            }
                        }
                        else if (option.Value.GetType() == prop.PropertyType)
                        {
                            try
                            {
                                prop.SetValue(payload, option.Value);
                            }
                            catch (Exception ex)
                            {
                                throw new CommandPropertyValidationException(option.Name, $"Could not assign value '{option.Value}' type '{option.Value?.GetType()}' to type '{prop.PropertyType}'");
                            }
                        }
                        else
                        {
                            try
                            {
                                prop.SetValue(payload, Convert.ChangeType(option.Value, prop.PropertyType));
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                                throw new CommandPropertyValidationException(option.Name, $"Could not cast value '{option.Value}' to type '{prop.PropertyType}'");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            return payload;
        }

        private async Task AddCommand(string command, string description, Type t, params SlashCommandOption[] slashCommandOptions)
        {
            SlashCommandBuilder commandBuilder = new SlashCommandBuilder()
                .WithName(command)
                .WithDescription(description);

            slashCommandOptions ??= [];

            foreach (SlashCommandOption option in slashCommandOptions)
            {
                commandBuilder.AddOption(option);
            }

            foreach (PropertyInfo property in t.GetProperties())
            {
                commandBuilder.TryAddOption(property);
            }

            foreach (SocketGuild? Guild in this._discordClient.Guilds)
            {
                await Guild.CreateApplicationCommandAsync(commandBuilder.Build());
            }
        }

        private Task DiscordClient_Log(LogMessage arg)
        {
            Console.WriteLine(arg.Message);
            return Task.CompletedTask;
        }

        private Task DiscordClient_Ready()
        {
            Console.WriteLine($"Connected as {this._discordClient.CurrentUser}");

            if (!this._readyTask.Task.IsCompleted)
            {
                this._readyTask.SetResult();
            }

            return Task.CompletedTask;
        }

        private async Task RemoveUserMessagesFromChannel(SocketTextChannel channel, ulong userid, int days = 7)
        {
            // Calculate the timestamp for one week ago
            var oneWeekAgo = DateTime.UtcNow.AddDays(0 - days);

            // Get the initial batch of messages in the channel
            await Task.Delay(500);
            var messages = await channel.GetMessagesAsync(limit: 100).FlattenAsync();

            while (messages.Any())
            {
                // Find messages authored by the user or mentioning the user within the last week
                var userMessages = messages.OfType<RestUserMessage>().Where(msg =>
                    msg.Author.Id == userid ||
                    msg.MentionedUsers.Any(mentionedUser => mentionedUser.Id == userid))
                    .Where(msg => msg.Timestamp.UtcDateTime >= oneWeekAgo);

                // Delete each matching message
                foreach (var message in userMessages)
                {
                    Console.WriteLine($"Deleting Message {message.Id}: {message.Content}");
                    await Task.Delay(500);
                    await message.DeleteAsync();
                }

                // Check if the oldest message in the current batch is older than one week
                var oldestMessage = messages.MinBy(msg => msg.Timestamp);

                if (oldestMessage != null)
                {
                    if (oldestMessage.Timestamp.UtcDateTime < oneWeekAgo)
                    {
                        break;
                    }

                    // Get the next batch of messages in the channel
                    await Task.Delay(500);
                    messages = await channel.GetMessagesAsync(oldestMessage, Direction.Before, limit: 100).FlattenAsync();
                }
            }
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (this._commandCallbacks.TryGetValue(command.CommandName, out Func<SocketSlashCommand, Task<string>> callback))
            {
                string result = "You should never see this message";

                await command.DeferAsync();

                try
                {
                    result = await callback.Invoke(command);
                }
                catch (CommandPropertyValidationException cex)
                {
                    result = $"{cex.PropertyName}: {cex.Message}";
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    result = ex.Message;
                }

                if (!string.IsNullOrWhiteSpace(result))
                {
                    try
                    {
                        await command.FollowupAsync(result, ephemeral: true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }
        }
    }
}