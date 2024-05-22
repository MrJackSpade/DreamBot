using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Dreambot.Database.Models;
using Dreambot.Database.Repositories;
using Dreambot.Database.Services;
using DreamBot.Collections;
using DreamBot.Constants;
using DreamBot.Extensions;
using DreamBot.Models;
using DreamBot.Models.Automatic;
using DreamBot.Models.Commands;
using DreamBot.Models.Events;
using DreamBot.Services;
using Loxifi;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace DreamBot
{
    internal class Program
    {
        private static readonly Dictionary<string, AutomaticService> _automaticServices = [];

        private static Configuration Configuration => StaticConfiguration.Load<Configuration>();

        private static readonly DiscordService _discordService;

        private static readonly ThreadService _threadService;

        private static readonly TaskCollection _userTasks;

        private static readonly Dictionary<string, List<Lora>> _loras = [];

        private static readonly GenerationService _generationService;

        static Program()
        {
            _threadService = new ThreadService();

            _discordService = new DiscordService(new DiscordServiceSettings()
            {
                Token = Configuration.Token
            });

            _generationService = new GenerationService(
                new GenerationDataRepository(Configuration.DatabaseConnectionString),
                new GenerationPropertyRepository(Configuration.DatabaseConnectionString),
                new GenerationRepository(Configuration.DatabaseConnectionString)
            );

            foreach (AutomaticEndPoint endpoint in Configuration.Endpoints)
            {
                AutomaticService _automaticService = new(new(endpoint.AutomaticHost, endpoint.AutomaticPort)
                {
                    AggressiveOptimizations = endpoint.AggressiveOptimizations
                });

                _automaticServices.Add(endpoint.DisplayName, _automaticService);
            }

            _userTasks = new TaskCollection(Configuration.MaxUserQueue);
        }

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

                await SendNotifications(generateImageCommand, placeholder);

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
                            await placeholder.Message.AddReactionsAsync(postGenEmotes.ToArray());
                        }

                        ForumPostService forumPostService = new(generateImageCommand.Channel);

                        if (await forumPostService.IsCreator(_discordService.User.Id))
                        {
                            await forumPostService.UpdateImage(genTaskResult.AutomaticTask!.State.CurrentImage!);
                        }

                        SaveGeneration(generateImageCommand, genTaskResult, lastImageName, placeholder.Message.Id);
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

        private static void SaveGeneration(GenerateImageCommand command, QueueTxt2ImgTaskResult genTaskResult, Guid lastImageName, ulong placeholderId)
        {
            GenerationDto generationDto = new()
            {
                ChannelId = command.Channel.Id,
                DateCreatedUtc = command.Command.CreatedAt.DateTime,
                UserId = command.User.Id,
                MessageId = placeholderId,
            };

            if (genTaskResult.AutomaticTask.State.Images.Length > 1)
            {
                throw new NotImplementedException();
            }

            foreach (string image in genTaskResult.AutomaticTask.State.Images)
            {
                generationDto.GenerationData.Add(new GenerationData()
                {
                    Data = image.FromBase64(),
                    FileGuid = lastImageName
                });
            }

            generationDto.AddProperty("Prompt", genTaskResult.AutomaticTask.Request.Prompt);
            generationDto.AddProperty("NegativePrompt", genTaskResult.AutomaticTask.Request.NegativePrompt);
            generationDto.AddProperty("Seed", genTaskResult.AutomaticTask.Request.Seed);
            generationDto.AddProperty("Width", genTaskResult.AutomaticTask.Request.Width);
            generationDto.AddProperty("Height", genTaskResult.AutomaticTask.Request.Height);
            generationDto.AddProperty("Steps", genTaskResult.AutomaticTask.Request.Steps);
            generationDto.AddProperty("SamplerName", genTaskResult.AutomaticTask.Request.SamplerName);

            _generationService.Insert(generationDto);
        }

        private static IChannel notificationChannel = null;

        public static SocketTextChannel NotificationChannel
        {
            get
            {
                notificationChannel ??= _discordService.GetChannelAsync(Configuration.NotificationChannelId);

                return notificationChannel as SocketTextChannel;
            }
        }

        private static async Task SendNotifications(GenerateImageCommand generateImageCommand, GenerationPlaceholder placeholder)
        {

            if (generateImageCommand.User is SocketGuildUser sgu)
            {
                bool isMod = sgu.Roles.Any(r => string.Equals(r.Name, "mod", StringComparison.OrdinalIgnoreCase));
                bool isMember = sgu.Roles.Any(r => string.Equals(r.Name, "contributor", StringComparison.OrdinalIgnoreCase));

                if(isMod || isMember)
                {
                    return;
                }
            }

            if (Configuration.NotificationChannelId != 0 && Configuration.NotificationTriggers.Length > 0)
            {
                IChannel c = generateImageCommand.Channel;

                SocketGuild g = _discordService.GetGuild(generateImageCommand.Command.GuildId.Value);

                foreach (string trigger in Configuration.NotificationTriggers)
                {
                    bool isMatch = false;

                    foreach (string promptPart in generateImageCommand.Prompt)
                    {
                        isMatch = isMatch || Regex.IsMatch(promptPart, trigger, RegexOptions.IgnoreCase);
                    }

                    if (isMatch)
                    {
                        StringBuilder sb = new();
                        //sb.AppendLine($"<@&{Configuration.NotificationRoleId}>");
                        sb.AppendLine($"https://discord.com/channels/{g.Id}/{c.Id}/{placeholder.Message.Id}");
                        sb.AppendLine($"<@{generateImageCommand.User.Id}>");
                        sb.AppendLine($"`{string.Join(", ", generateImageCommand.Prompt)}`");
                        await NotificationChannel?.SendMessageAsync(sb.ToString());
                    }
                }
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

        private static async Task<string> PurgeUser(PurgeCommand command)
        {
            if (!command.Command.GuildId.HasValue)
            {
                return "Not in guild";
            }

            // Get all text channels in the server
            SocketGuild guild = _discordService.GetGuild(command.Command.GuildId.Value);

            await _discordService.RemoveUserMessages(guild, command.TargetUserId, command.Days);

            return "Completed";
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

            channelConfiguration.Prompt = defaultStyle.DisplayName;
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

            await _discordService.AddCommand<ShutdownCommand>("Shutdown", "Disables the bot for all users", Shutdown);

            await _discordService.AddCommand<CreateThreadCommand>("new_thread", "Creates a new thread for image generation", CreateThread,
                new SlashCommandOption("default_style", "The type of model to use for image generation", true, Configuration.Styles.Select(m => m.DisplayName).ToArray()));

            _discordService.ReactionAdded += ReactionAdded;

            await Task.Delay(-1);
        }

        private static async Task<string> Shutdown(ShutdownCommand command)
        {
            if (!command.Command.GuildId.HasValue)
            {
                return "Not in guild";
            }

            // Get all text channels in the server
            SocketGuild guild = _discordService.GetGuild(command.Command.GuildId.Value);

            await _discordService.Ban(guild, command.User.Id, "Attempted to execute honeypot command");

            StringBuilder sb = new();
            sb.AppendLine($"<@{command.User.Id}>");
            sb.AppendLine($"`Attempted to execute honeypot command`");
            await NotificationChannel?.SendMessageAsync(sb.ToString());
            return string.Empty;
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