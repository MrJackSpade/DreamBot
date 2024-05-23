using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Dreambot.Plugins.Interfaces;
using DreamBot.Collections;
using DreamBot.Constants;
using DreamBot.Extensions;
using DreamBot.Models;
using DreamBot.Models.Automatic;
using DreamBot.Models.Commands;
using DreamBot.Models.Events;
using DreamBot.Plugins.EventArgs;
using DreamBot.Services;
using DreamBot.Shared.Interfaces;
using DreamBot.Shared.Loggers;
using DreamBot.Shared.Models;
using Loxifi;
using Newtonsoft.Json;
using System.Reflection;

namespace DreamBot
{
    internal class Program
    {
        private static readonly Dictionary<string, AutomaticService> _automaticServices = [];

        private static readonly DiscordService _discordService;

        private static readonly ILogger _logger = new ConsoleLogger();

        private static readonly Dictionary<string, List<Lora>> _loras = [];

        private static readonly PluginService _pluginService;

        private static readonly ThreadService _threadService;

        private static readonly TaskCollection _userTasks;

        static Program()
        {
            _threadService = new ThreadService();

            _discordService = new DiscordService(new DiscordServiceSettings()
            {
                Token = Configuration.Token
            });

            foreach (AutomaticEndPoint endpoint in Configuration.Endpoints)
            {
                AutomaticService _automaticService = new(new(endpoint.AutomaticHost, endpoint.AutomaticPort)
                {
                    AggressiveOptimizations = endpoint.AggressiveOptimizations
                });

                _automaticServices.Add(endpoint.DisplayName, _automaticService);
            }

            _userTasks = new TaskCollection(Configuration.MaxUserQueue);

            _pluginService = new PluginService(_logger, _discordService);
        }

        private static Configuration Configuration => StaticConfiguration.Load<Configuration>("Configurations\\DreamBot\\Configuration.json");

        public static Task<string> GenerateImage(GenerateImageCommand generateImageCommand)
        {
            if (generateImageCommand.Channel is null || generateImageCommand.User is null)
            {
                return Task.FromResult("Invalid Request");
            }

            ulong channelId = generateImageCommand.Channel.Id;
            ulong requesterId = generateImageCommand.User.Id;

            ChannelConfiguration channelConfiguration = ConfigurationService.GetChannelConfiguration(channelId);

            if (string.IsNullOrWhiteSpace(channelConfiguration.DefaultStyle))
            {
                return Task.FromResult("Channel does not have Default Style set");
            }

            AutomaticEndPoint? matchingEndpoint = Configuration.Endpoints.Where(e => e.SupportedStyleNames.Contains(channelConfiguration.DefaultStyle)).FirstOrDefault();

            if (matchingEndpoint is null)
            {
                return Task.FromResult($"No endpoint supporting model '{channelConfiguration.DefaultStyle}' found in configuration");
            }

            if (!_automaticServices.TryGetValue(matchingEndpoint.DisplayName, out AutomaticService? automaticService))
            {
                return Task.FromResult($"No AutomaticService found with display name '{matchingEndpoint.DisplayName}'");
            }

            Resolution resolution = channelConfiguration.Resolutions[generateImageCommand.AspectRatio.ToString()];

            List<string> prompt = generateImageCommand.Prompt;

            List<String> neg_prompt = generateImageCommand.NegativePrompt;

            foreach (string lora in generateImageCommand.Lora)
            {
                prompt.Insert(0, $"<lora:{lora}:{generateImageCommand.LoraStrength}> ");
            }

            if (generateImageCommand.ApplyDefaultStyles)
            {
                prompt = prompt.ApplyTemplate(channelConfiguration.Prompt);
                neg_prompt = neg_prompt.ApplyTemplate(channelConfiguration.NegativePrompt);
            }

            Txt2Img settings = new()
            {
                Prompt = string.Join(",", prompt.Distinct()),
                NegativePrompt = string.Join(",", neg_prompt.Distinct()),
                Width = resolution.Width,
                Height = resolution.Height,
                Seed = generateImageCommand.Seed
            };

            List<IEmote> postGenEmotes = [
                Emojis.FEAR,
                Emojis.LOLICE
            ];

            CancellationTokenSource cts = new();

            if (!_userTasks.TryReserve(requesterId, out QueuedTask? queuedTask))
            {
                return Task.FromResult($"You have already reached the max queue tasks of {Configuration.MaxUserQueue}. Try again later.");
            }

            queuedTask.OnCancelled += (s, e) => cts.Cancel();

            QueueTxt2ImgTaskResult genTaskResult = automaticService.Txt2Image(settings, cts.Token);

            queuedTask.AutomaticTask = genTaskResult.AutomaticTask;

            _threadService.Enqueue(async () =>
            {
                string title = $"*<@{requesterId}> is dreaming of **{string.Join(", ", generateImageCommand.Prompt)}***";

                GenerationPlaceholder placeholder = new(await generateImageCommand.Command.FollowupAsync(title));

                queuedTask.MessageId = placeholder.Message.Id;

                //Add emoji after delete event is wired up for safety and lazyiness;
                await placeholder.Message.AddReactionsAsync(
                [
                    Emojis.TRASH,
                ]);

                try
                {
                    DateTime completed = DateTime.MinValue;

                    string finalBody = string.Empty;

                    DateTime lastChange = DateTime.MinValue;

                    Guid lastImageName = Guid.Empty;

                    genTaskResult.AutomaticTask.ProgressUpdated += async (s, progress) =>
                    {
                        try
                        {
                            if (!string.IsNullOrWhiteSpace(progress.Exception))
                            {
                                finalBody = progress.Exception;
                                await placeholder.TryUpdate(title, finalBody, progress.CurrentImage);
                            }
                            else if (progress.Completed)
                            {
                                settings.Seed = progress.Info!.Seed;
                                completed = DateTime.Now;
                                string mention = $"👤 <@{requesterId}>";
                                finalBody = settings.ToDiscordString(completed - genTaskResult.QueueTime);
                                lastImageName = await placeholder.TryUpdate(string.Empty, mention + finalBody, progress.CurrentImage);
                            }
                            else
                            {
                                if (lastChange.AddMilliseconds(Configuration.UpdateTimeoutMs) > DateTime.Now)
                                {
                                    return;
                                }

                                string displayProgress = $"{(int)(progress.Progress * 100)}% - ETA: {(int)progress.EtaRelative} seconds";

                                lastImageName = await placeholder.TryUpdate(title, displayProgress, progress.CurrentImage);

                                if (lastImageName != Guid.Empty)
                                {
                                    lastChange = DateTime.Now;
                                }
                            }

                            Console.WriteLine(progress.Progress.ToString());
                        }
                        catch (Exception ex)
                        {
                        }
                    };

                    IEmote starEmote = Emojis.STAR;

                    foreach (string prompt_part in generateImageCommand.Prompt.SelectMany(s => s.Split(' ', '_')).Where(s => s.Length > 2).Select(s => s.ToLower()).Distinct())
                    {
                        if (Emoji.TryParse($":{prompt_part}:", out var emote))
                        {
                            starEmote = emote;
                            break;
                        }
                    }

                    postGenEmotes.Insert(0, starEmote);

                    genTaskResult.AutomaticTask.Wait();

                    if (!genTaskResult.Cancelled && string.IsNullOrWhiteSpace(genTaskResult.AutomaticTask.State.Exception))
                    {
                        if (!string.IsNullOrWhiteSpace(genTaskResult.AutomaticTask?.State?.CurrentImage))
                        {
                            await placeholder.Message.AddReactionsAsync([.. postGenEmotes]);
                        }

                        ForumPostService forumPostService = new(generateImageCommand.Channel);

                        if (await forumPostService.IsCreator(_discordService.User.Id))
                        {
                            await forumPostService.UpdateImage(genTaskResult.AutomaticTask!.State.CurrentImage!);
                        }

                        await SendPostGenerationEvent(generateImageCommand, genTaskResult, placeholder, lastImageName);
                    }
                }
                finally
                {
                    _userTasks.Remove(queuedTask);
                }
            });

            int position = genTaskResult.QueuePosition;

            if (position > 1)
            {
                return Task.FromResult($"Generation queued, position [{position}]");
            }
            else
            {
                return Task.FromResult(string.Empty);
            }
        }

        public static async Task ReactionAdded(ReactionEventArgs args)
        {
            IMessage message = await args.UserMessage.GetOrDownloadAsync();

            if (message is null)
            {
                return;
            }

            if (message.Author.Id != _discordService.User.Id)
            {
                return;
            }

            switch (args.SocketReaction.Emote.Name)
            {
                case Emojis.STR_TRASH:
                    await TryDeleteMessage(args);
                    break;
            }
        }

        public static Task<string> UpdateSettings(UpdateSettingsCommand socketSlashCommand)
        {
            if (socketSlashCommand.Channel is null || socketSlashCommand.User is null)
            {
                return Task.FromResult("Invalid Request");
            }

            ChannelConfiguration channelConfiguration = ConfigurationService.GetChannelConfiguration(socketSlashCommand.Channel.Id);

            if (socketSlashCommand.LandscapeWidth != 0)
            {
                channelConfiguration.Resolutions[nameof(AspectRatio.Landscape)].Width = socketSlashCommand.LandscapeWidth;
            }

            if (socketSlashCommand.LandscapeHeight != 0)
            {
                channelConfiguration.Resolutions[nameof(AspectRatio.Landscape)].Height = socketSlashCommand.LandscapeHeight;
            }

            if (socketSlashCommand.PortraitWidth != 0)
            {
                channelConfiguration.Resolutions[nameof(AspectRatio.Portrait)].Width = socketSlashCommand.PortraitWidth;
            }

            if (socketSlashCommand.PortraitHeight != 0)
            {
                channelConfiguration.Resolutions[nameof(AspectRatio.Portrait)].Height = socketSlashCommand.PortraitHeight;
            }

            if (socketSlashCommand.SquareWidth != 0)
            {
                channelConfiguration.Resolutions[nameof(AspectRatio.Square)].Width = socketSlashCommand.SquareWidth;
            }

            if (socketSlashCommand.SquareHeight != 0)
            {
                channelConfiguration.Resolutions[nameof(AspectRatio.Square)].Height = socketSlashCommand.SquareHeight;
            }

            if (socketSlashCommand.Prompt != null)
            {
                channelConfiguration.Prompt = socketSlashCommand.Prompt;
            }

            if (socketSlashCommand.NegativePrompt != null)
            {
                channelConfiguration.NegativePrompt = socketSlashCommand.NegativePrompt;
            }

            if (!string.IsNullOrWhiteSpace(socketSlashCommand.DefaultStyle))
            {
                channelConfiguration.DefaultStyle = socketSlashCommand.DefaultStyle;
            }

            ConfigurationService.SaveChannelConfiguration(socketSlashCommand.Channel.Id, channelConfiguration);

            string settingValue = JsonConvert.SerializeObject(channelConfiguration, Formatting.Indented);

            return Task.FromResult($"```{settingValue}```");
        }

        private static async Task<string> CreateThread(CreateThreadCommand command)
        {
            if (command.Channel == null)
            {
                return "Null channel";
            }

            ulong channelid = command.Channel.Id;

            if (Configuration.ThreadCreationChannels is null)
            {
                return "Configuration contains null value for ThreadCreationChannels";
            }

            if (!Configuration.ThreadCreationChannels.Contains(channelid))
            {
                return "Can not create a thread in this channel";
            }

            if (command.Channel is not SocketThreadChannel stc)
            {
                return "Current channel is not SocketThreadChannel";
            }

            if (stc.ParentChannel is not SocketForumChannel sfc)
            {
                return "Parent channel is not SocketForumChannel";
            }

            if (Configuration.Styles.FirstOrDefault(s => s.DisplayName == command.DefaultStyle) is not Style defaultStyle)
            {
                return $"No configured style with name {command.DefaultStyle} found";
            }

            string desc = command.Description;

            if (string.IsNullOrWhiteSpace(desc))
            {
                desc = command.Title;
            }

            desc = $"{desc} by <@{command.User.Id}>";

            RestThreadChannel ric = await sfc.CreatePostAsync(command.Title, text: desc);

            ChannelConfiguration channelConfiguration = ConfigurationService.GetChannelConfiguration(ric.Id);

            channelConfiguration.DefaultStyle = command.DefaultStyle;

            channelConfiguration.Prompt = defaultStyle.PositivePrompt;
            channelConfiguration.NegativePrompt = defaultStyle.NegativePrompt;

            ConfigurationService.SaveChannelConfiguration(ric.Id, channelConfiguration);

            string message = $"Your thread was created > https://discord.com/channels/{channelid}/{ric.Id}";

            await foreach (IReadOnlyCollection<RestMessage>? messages in ric.GetMessagesAsync(100))
            {
                if (messages.FirstOrDefault() is RestUserMessage rum)
                {
                    await rum.PinAsync();
                    return message;
                }
            }

            return message;
        }

        private static async Task Main(string[] args)
        {
            await _discordService.Connect();
            await _pluginService.LoadPlugins();

            foreach (AutomaticEndPoint endpoint in Configuration.Endpoints)
            {
                AutomaticService _automaticService = _automaticServices[endpoint.DisplayName];

                Lora[] loras = [];

                try
                {
                    loras = await _automaticService.GetLoras()!;
                }
                catch (Exception ex)
                {
                    continue;
                }

                foreach (string modelStyle in endpoint.SupportedStyleNames)
                {
                    if (!_loras.TryGetValue(modelStyle, out List<Lora>? styleLoras))
                    {
                        styleLoras = [.. loras];
                        _loras[modelStyle] = styleLoras;
                    }
                    else
                    {
                        foreach (Lora l in styleLoras.ToList())
                        {
                            if (!loras.Contains(l))
                            {
                                styleLoras.Remove(l);
                                Console.WriteLine($"Removing LORA {l.Name} not supported by endpoint {endpoint.DisplayName}");
                            }
                        }
                    }
                }
            }

            await _discordService.AddCommand<GenerateImageCommand>("Dream", "Generates an image", GenerateImage);

            await _discordService.AddCommand<PurgeCommand>("Purge", "Purges all content created by a user", PurgeUser);

            await _discordService.AddCommand<UpdateSettingsCommand>("Settings", "Updates Settings", UpdateSettings);

            await _discordService.AddCommand<CreateThreadCommand>("new_thread", "Creates a new thread for image generation", CreateThread,
                new SlashCommandOption("default_style", "The type of model to use for image generation", true, Configuration.Styles.Select(m => m.DisplayName).ToArray()));

            foreach (ICommandProvider commandProvider in _pluginService.CommandProviders)
            {
                Type parameterType = commandProvider.GetType()
                                                    .GetInterface(typeof(ICommandProvider<>).Name)!
                                                    .GetGenericArguments()[0];

                MethodInfo invocationMethod = commandProvider.GetType().GetMethod(nameof(ICommandProvider<object>.OnCommand))!;

                await _discordService.AddCommand(commandProvider.Command,
                                                 commandProvider.Description,
                                                 parameterType,
                                                 c => (Task<string>)invocationMethod.Invoke(commandProvider, [c])!);
            }

            _discordService.ReactionAdded += ReactionAdded;

            await Task.Delay(-1);
        }

        private static async Task<string> PurgeUser(PurgeCommand command)
        {
            if (!command.Command.GuildId.HasValue)
            {
                return "Not in guild";
            }

            // Get all text channels in the server
            SocketGuild guild = await _discordService.GetGuildAsync(command.Command.GuildId.Value);

            await _discordService.RemoveUserMessages(guild, command.TargetUserId, command.Days);

            return "Completed";
        }

        private static async Task SendPostGenerationEvent(GenerateImageCommand generateImageCommand, QueueTxt2ImgTaskResult genTaskResult, GenerationPlaceholder placeholder, Guid lastImageName)
        {
            PostGenerationEventArgs postGenerationEventArgs = new()
            {
                Message = placeholder.Message,
                DateCreated = generateImageCommand.Command.CreatedAt.DateTime,
                Images =
                [
                    new GeneratedImage(lastImageName, genTaskResult.AutomaticTask!.State.CurrentImage!)
                ],
                Guild = await _discordService.GetGuildAsync(generateImageCommand.Command.GuildId.Value),
                User = generateImageCommand.User,
                GenerationParameters = new GenerationParameters()
                {
                    Height = genTaskResult.AutomaticTask.Request.Height,
                    Width = genTaskResult.AutomaticTask.Request.Width,
                    Seed = genTaskResult.AutomaticTask.Request.Seed,
                    Prompt = new Prompt(genTaskResult.AutomaticTask.Request.Prompt),
                    NegativePrompt = new Prompt(genTaskResult.AutomaticTask.Request.NegativePrompt),
                    SamplerName = genTaskResult.AutomaticTask.Request.SamplerName,
                    Steps = genTaskResult.AutomaticTask.Request.Steps
                }
            };

            _pluginService.PostGenerationEvent(postGenerationEventArgs);
        }

        private static async Task TryDeleteMessage(ReactionEventArgs args)
        {
            try
            {
                // Get the full message from the cache or fetch it from the API
                IUserMessage message = await args.UserMessage.GetOrDownloadAsync();

                // Check if the message has exactly one user mentioned

                if (message.MentionedUserIds.Count == 1)
                {
                    // Get the mentioned user
                    ulong mentionedUser = message.MentionedUserIds.FirstOrDefault();

                    // Check if the user who added the reaction is the same as the mentioned user
                    if (mentionedUser != 0 && args.SocketReaction.UserId == mentionedUser)
                    {
                        _userTasks.TryCancel(message.Id);
                        // Delete the message
                        await message.DeleteAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during the process
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}