using Discord;
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
        private static readonly AutomaticService _automaticService;

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

            _automaticService = new AutomaticService(new(_configuration.AutomaticHost, _configuration.AutomaticPort)
            {
                AggressiveOptimizations = _configuration.AggressiveOptimizations
            });

            _userTasks = new TaskCollection(_configuration.MaxUserQueue);
        }

        public static Task<string> GenerateImage(GenerateImageCommand socketSlashCommand)
        {
            if (socketSlashCommand.ChannelId is null || socketSlashCommand.User is null)
            {
                return Task.FromResult("Invalid Request");
            }

            ulong requesterId = socketSlashCommand.User.Id;

            ChannelConfiguration channelConfiguration = ConfigurationService.GetChannelConfiguration(socketSlashCommand.ChannelId.Value);

            Resolution resolution = channelConfiguration.Resolutions[socketSlashCommand.AspectRatio.ToString()];

            Txt2Img settings = new()
            {
                Prompt = socketSlashCommand.Prompt.ApplyTemplate(channelConfiguration.Prompt),
                NegativePrompt = socketSlashCommand.NegativePrompt.ApplyTemplate(channelConfiguration.NegativePrompt),
                Width = resolution.Width,
                Height = resolution.Height,
                Seed = socketSlashCommand.Seed
            };

            CancellationTokenSource cts = new();

            if (!_userTasks.TryReserve(requesterId, out QueuedTask? queuedTask))
            {
                return Task.FromResult($"You have already reached the max queue tasks of {_configuration.MaxUserQueue}. Try again later.");
            }

            queuedTask.OnCancelled += (s, e) => cts.Cancel();

            QueueTxt2ImgTaskResult genTaskResult = _automaticService.Txt2Image(settings, cts.Token);

            queuedTask.AutomaticTask = genTaskResult.AutomaticTask;

            _threadService.Enqueue(async () =>
            {
                string title = $"*<@{requesterId}> is dreaming of **{socketSlashCommand.Prompt}***";

                GenerationEmbed placeholder = await _discordService.CreateMessage(title, socketSlashCommand.ChannelId.Value);

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
                            await socketSlashCommand.User.SendFileAsync(finalBody, genTaskResult.AutomaticTask.State.CurrentImage);
                        }
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
            if (socketSlashCommand.ChannelId is null || socketSlashCommand.User is null)
            {
                return Task.FromResult("Invalid Request");
            }

            ChannelConfiguration channelConfiguration = ConfigurationService.GetChannelConfiguration(socketSlashCommand.ChannelId.Value);

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

            ConfigurationService.SaveChannelConfiguration(socketSlashCommand.ChannelId.Value, channelConfiguration);

            string settingValue = JsonConvert.SerializeObject(channelConfiguration, Formatting.Indented);

            return Task.FromResult($"```{settingValue}```");
        }

        private static async Task Main(string[] args)
        {
            await _discordService.Connect();

            await _discordService.AddCommand<GenerateImageCommand>("Dream", "Generates an image", GenerateImage);

            await _discordService.AddCommand<UpdateSettingsCommand>("Settings", "Updates Settings", UpdateSettings);

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
    }
}