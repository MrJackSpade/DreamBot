using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DreamBot.Exceptions;
using DreamBot.Models;
using DreamBot.Models.Automatic;
using DreamBot.Models.Commands;
using DreamBot.Extensions;
using DreamBot.Models.Events;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.Serialization;

namespace DreamBot.Services
{
    internal class DiscordService
    {
        public EventHandler<Txt2ImgProgress> ProgressUpdated;

        public Func<ReactionEventArgs, Task> ReactionAdded;

        private readonly Dictionary<string, Func<SocketSlashCommand, Task<string>>> _commandCallbacks = [];

        private readonly DiscordSocketClient _discordClient;

        private readonly TaskCompletionSource _readyTask = new();

        private readonly DiscordServiceSettings _settings;

        public IUser User { get; set; }

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

        public static T CastType<T>(SocketSlashCommand source) where T : BaseCommand, new()
        {
            T payload = new()
            {
                Command = source
            };

            Dictionary<string, PropertyInfo> propertyDict = [];

            foreach (PropertyInfo pi in typeof(T).GetProperties())
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
                        if (prop.PropertyType.IsEnum)
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

        public async Task AddCommand<T>(string command, string description, Func<T, Task<string>> action, params SlashCommandOption[] slashCommandOptions) where T : BaseCommand, new()
        {
            command = command.ToLower();

            this._commandCallbacks.Add(command, (c) => action.Invoke(CastType<T>(c)));

            SlashCommandBuilder commandBuilder = new SlashCommandBuilder()
                .WithName(command)
                .WithDescription(description);

            slashCommandOptions ??= [];

            foreach (SlashCommandOption option in slashCommandOptions)
            {
                commandBuilder.AddOption(option);
            }

            foreach (PropertyInfo property in typeof(T).GetProperties())
            {
                commandBuilder.TryAddOption(property);
            }

            foreach (SocketGuild? Guild in this._discordClient.Guilds)
            {
                await Guild.CreateApplicationCommandAsync(commandBuilder.Build());
            }
        }

        public async Task Connect()
        {
            await this._discordClient.LoginAsync(TokenType.Bot, this._settings.Token);
            await this._discordClient.StartAsync();
            await this._readyTask.Task;
            this.User = this._discordClient.CurrentUser;
        }

        public async Task<GenerationPlaceholder> CreateMessage(string title, ulong channel)
        {
            IChannel socketChannel = await this._discordClient.GetChannelAsync(channel);

            return await this.CreateMessage(title, socketChannel);
        }

        public async Task<GenerationPlaceholder> CreateMessage(string title, IChannel socketChannel)
        {
            if (socketChannel is SocketTextChannel stc)
            {
                RestUserMessage message = await stc.SendMessageAsync(title);

                return new GenerationPlaceholder()
                {
                    Message = message
                };
            }

            throw new Exception();
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

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (this._commandCallbacks.TryGetValue(command.CommandName, out Func<SocketSlashCommand, Task<string>> callback))
            {
                string result = "You should never see this message";

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

                await command.RespondAsync(result, ephemeral: true);
            }
        }
    }
}