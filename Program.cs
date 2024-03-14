using Discord;
using Discord.Rest;
using Discord.WebSocket;
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

namespace DreamBot
{
    internal class Program
    {
        private static readonly Dictionary<string, AutomaticService> _automaticServices = [];

        private static readonly Configuration _configuration;

        private static readonly DiscordService _discordService;

        private static readonly ThreadService _threadService;

        private static readonly TaskCollection _userTasks;

        static Program()
        {
            _threadService = new ThreadService();

            _configuration = StaticConfiguration.Load<Configuration>();

            _discordService = new DiscordService(new DiscordServiceSettings()
            {
                Token = _configuration.Token
            });

            foreach (AutomaticEndPoint endpoint in _configuration.Endpoints)
            {
                AutomaticService _automaticService = new(new(endpoint.AutomaticHost, endpoint.AutomaticPort)
                {
                    AggressiveOptimizations = endpoint.AggressiveOptimizations
                });

                _automaticServices.Add(endpoint.DisplayName, _automaticService);
            }

            _userTasks = new TaskCollection(_configuration.MaxUserQueue);
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

            AutomaticEndPoint? matchingEndpoint = _configuration.Endpoints.Where(e => e.SupportedStyleNames.Contains(channelConfiguration.DefaultStyle)).FirstOrDefault();

            if (matchingEndpoint is null)
            {
                return Task.FromResult($"No endpoint supporting model '{channelConfiguration.DefaultStyle}' found in configuration");
            }

            if (!_automaticServices.TryGetValue(matchingEndpoint.DisplayName, out AutomaticService? automaticService))
            {
                return Task.FromResult($"No AutomaticService found with display name '{matchingEndpoint.DisplayName}'");
            }

            Resolution resolution = channelConfiguration.Resolutions[generateImageCommand.AspectRatio.ToString()];

            Txt2Img settings = new()
            {
                Prompt = generateImageCommand.Prompt.ApplyTemplate(channelConfiguration.Prompt),
                NegativePrompt = generateImageCommand.NegativePrompt.ApplyTemplate(channelConfiguration.NegativePrompt),
                Width = resolution.Width,
                Height = resolution.Height,
                Seed = generateImageCommand.Seed
            };

            CancellationTokenSource cts = new();

            if (!_userTasks.TryReserve(requesterId, out QueuedTask? queuedTask))
            {
                return Task.FromResult($"You have already reached the max queue tasks of {_configuration.MaxUserQueue}. Try again later.");
            }

            queuedTask.OnCancelled += (s, e) => cts.Cancel();

            QueueTxt2ImgTaskResult genTaskResult = automaticService.Txt2Image(settings, cts.Token);

            queuedTask.AutomaticTask = genTaskResult.AutomaticTask;

            _threadService.Enqueue(async () =>
            {
                string title = $"*<@{requesterId}> is dreaming of **{generateImageCommand.Prompt}***";

                GenerationPlaceholder placeholder = await _discordService.CreateMessage(title, generateImageCommand.Channel);

                queuedTask.MessageId = placeholder.Message.Id;

                //Add emoji after delete event is wired up for safety and lazyiness;
                await placeholder.Message.AddReactionsAsync(
                [
                    new Emoji(Emojis.TRASH),
                ]);

                try
                {
                    DateTime completed = DateTime.MinValue;

                    string finalBody = string.Empty;

                    DateTime lastChange = DateTime.MinValue;

                    genTaskResult.AutomaticTask.ProgressUpdated += async (s, progress) =>
                    {
                        try
                        {
                            if (progress.Completed)
                            {
                                settings.Seed = progress.Info!.Seed;
                                completed = DateTime.Now;
                                string mention = $"👤 <@{requesterId}>";
                                finalBody = settings.ToDiscordString(completed - genTaskResult.QueueTime);
                                await placeholder.TryUpdate(string.Empty, mention + finalBody, progress.CurrentImage);
                            }
                            else
                            {
                                if (lastChange.AddMilliseconds(_configuration.UpdateTimeoutMs) > DateTime.Now)
                                {
                                    return;
                                }

                                string displayProgress = $"{(int)(progress.Progress * 100)}% - ETA: {(int)progress.EtaRelative} seconds";
                                bool changed = await placeholder.TryUpdate(title, displayProgress, progress.CurrentImage);

                                if (changed)
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

                    genTaskResult.AutomaticTask.Wait();

                    if (!genTaskResult.Cancelled)
                    {
                        if (!string.IsNullOrWhiteSpace(genTaskResult.AutomaticTask?.State?.CurrentImage))
                        {
                            await placeholder.Message.AddReactionAsync(new Emoji("⭐"));
                            //await generateImageCommand.User.SendFileAsync(finalBody, genTaskResult.AutomaticTask.State.CurrentImage);
                        }

                        await UpdateForumImage(generateImageCommand.Channel, genTaskResult.AutomaticTask.State.CurrentImage);
                    }
                }
                finally
                {
                    _userTasks.Remove(queuedTask);
                }
            });

            int position = genTaskResult.QueuePosition;

            if (position <= 1)
            {
                return Task.FromResult("Generating...");
            }
            else
            {
                return Task.FromResult($"Generation queued, position [{position}]");
            }
        }

        public static async Task ReactionAdded(ReactionEventArgs args)
        {
            IMessage message = await args.UserMessage.GetOrDownloadAsync();

            if(message is null)
            {
                return;
            }

            if (message.Author.Id != _discordService.User.Id)
            {
                return;
            }

            switch (args.SocketReaction.Emote.Name)
            {
                case Emojis.TRASH:
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

            if (_configuration.ThreadCreationChannels is null)
            {
                return "Configuration contains null value for ThreadCreationChannels";
            }

            if (!_configuration.ThreadCreationChannels.Contains(channelid))
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

            if (_configuration.Styles.FirstOrDefault(s => s.DisplayName == command.DefaultStyle) is not Style defaultStyle)
            {
                return $"No configured style with name {command.DefaultStyle} found";
            }


            string desc = command.Description;

            if(string.IsNullOrWhiteSpace(desc))
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

            await _discordService.AddCommand<GenerateImageCommand>("Dream", "Generates an image", GenerateImage);

            await _discordService.AddCommand<UpdateSettingsCommand>("Settings", "Updates Settings", UpdateSettings);

            await _discordService.AddCommand<CreateThreadCommand>("new_thread", "Creates a new thread for image generation", CreateThread, 
                new SlashCommandOption("default_style", "The type of model to use for image generation", true, _configuration.Styles.Select(m => m.DisplayName).ToArray()));

            _discordService.ReactionAdded += ReactionAdded;

            await Task.Delay(-1);
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

        private static async Task UpdateForumImage(IChannel channel, string? currentImage)
        {
            //Check if the channel is a forum channel
            if (channel is SocketThreadChannel threadChannel)
            {
                IReadOnlyCollection<IMessage> pinned = await threadChannel.GetPinnedMessagesAsync();

                IMessage firstMessage = pinned.FirstOrDefault();

                if (firstMessage is not RestUserMessage sum)
                {
                    return;
                }

                if (firstMessage.Author.Id != _discordService.User.Id)
                {
                    return;
                }

                using DisposableFileAttachment disposableFileAttachment = ImageService.CreateThumb(currentImage, "preview.png");

                await sum.ModifyAsync(m => m.Attachments = new FileAttachment[] { disposableFileAttachment.Attachment });
            }
        }
    }
}