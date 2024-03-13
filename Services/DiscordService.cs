using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DreamBot.Exceptions;
using DreamBot.Models;
using DreamBot.Models.Automatic;
using DreamBot.Models.Commands;
using DreamBot.Models.Events;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

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

        public DiscordService(DiscordServiceSettings settings)
        {
            _settings = settings;
            _discordClient = new DiscordSocketClient();
            _discordClient.Log += this.DiscordClient_Log;
            _discordClient.Ready += this.DiscordClient_Ready;
            _discordClient.SlashCommandExecuted += this.SlashCommandHandler;
            _discordClient.ReactionAdded += async (cache, channel, reaction) =>
            {
                if (ReactionAdded != null)
                {
                    await ReactionAdded.Invoke(new ReactionEventArgs(cache, channel, reaction));
                }
            };
        }

        public IUser User { get; set; }

        public static T CastType<T>(SocketSlashCommand source) where T : BaseCommand, new()
        {
            T payload = new()
            {
                User = source.User,
                ChannelId = source.ChannelId
            };

            Dictionary<string, PropertyInfo> propertyDict = new();

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

        public async Task AddCommand<T>(string command, string description, Func<T, Task<string>> action) where T : BaseCommand, new()
        {
            command = command.ToLower();

            _commandCallbacks.Add(command, (c) => action.Invoke(CastType<T>(c)));

            SlashCommandBuilder commandBuilder = new SlashCommandBuilder()
                .WithName(command)
                .WithDescription(description);

            foreach (PropertyInfo property in typeof(T).GetProperties())
            {
                if (property.DeclaringType != property.ReflectedType)
                {
                    continue;
                }

                SlashCommandOptionBuilder optionBuilder = new();

                string oname = property.Name.ToLower();
                string odescription = property.Name;

                if (property.GetCustomAttribute<DisplayAttribute>() is DisplayAttribute d)
                {
                    if (!string.IsNullOrWhiteSpace(d.Name))
                    {
                        oname = d.Name.ToLower();
                    }

                    if (!string.IsNullOrWhiteSpace(d.Description))
                    {
                        odescription = d.Description;
                    }
                }

                optionBuilder = optionBuilder.WithName(oname);
                optionBuilder = optionBuilder.WithDescription(odescription);

                if (property.GetCustomAttribute<RequiredAttribute>() is RequiredAttribute r)
                {
                    optionBuilder = optionBuilder.WithRequired(true);
                }

                if (property.PropertyType.IsEnum)
                {
                    optionBuilder = optionBuilder.WithType(ApplicationCommandOptionType.String);

                    foreach (Enum value in Enum.GetValues(property.PropertyType))
                    {
                        optionBuilder.AddChoice(value.ToString(), value.ToString());
                    }
                }
                else
                {
                    switch (property.PropertyType.Name)
                    {
                        case nameof(String):
                            optionBuilder = optionBuilder.WithType(ApplicationCommandOptionType.String);
                            break;

                        case nameof(Int32):
                        case nameof(Int64):
                        case nameof(UInt32):
                        case nameof(UInt64):
                            optionBuilder = optionBuilder.WithType(ApplicationCommandOptionType.Integer);
                            break;

                        default:
                            throw new NotImplementedException();
                    }
                }

                commandBuilder.AddOption(optionBuilder);
            }

            foreach (SocketGuild? Guild in _discordClient.Guilds)
            {
                await Guild.CreateApplicationCommandAsync(commandBuilder.Build());
            }
        }

        public async Task Connect()
        {
            await _discordClient.LoginAsync(TokenType.Bot, _settings.Token);
            await _discordClient.StartAsync();
            await _readyTask.Task;
            User = _discordClient.CurrentUser;
        }

        public async Task<GenerationEmbed> CreateMessage(string title, ulong channel)
        {
            IChannel socketChannel = await _discordClient.GetChannelAsync(channel);

            if (socketChannel is SocketTextChannel stc)
            {
                RestUserMessage message = await stc.SendMessageAsync(title);

                return new GenerationEmbed()
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
            Console.WriteLine($"Connected as {_discordClient.CurrentUser}");

            if (!_readyTask.Task.IsCompleted)
            {
                _readyTask.SetResult();
            }

            return Task.CompletedTask;
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (_commandCallbacks.TryGetValue(command.CommandName, out Func<SocketSlashCommand, Task<string>> callback))
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