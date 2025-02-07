using Discord;
using Discord.WebSocket;
using DreamBot.Collections;
using DreamBot.Constants;
using DreamBot.Extensions;
using DreamBot.Models;
using DreamBot.Models.Automatic;
using DreamBot.Services;
using DreamBot.Shared.Extensions;
using DreamBot.Shared.Interfaces;
using DreamBot.Shared.Models;
using DreamBot.Shared.Utils;

namespace DreamBot.Plugins.Dream
{
    public class SingleGenerationTask
    {
        public EventHandler<SingleGenerationTask> OnCompleted;

        private readonly IReadOnlyDictionary<string, IAutomaticService> _automaticServices;

        private readonly ulong _channelId;

        private readonly Configuration _configuration;

        private readonly IDiscordService _discordService;

        private readonly DreamCommand _dreamCommand;

        private readonly ulong _requesterId;

        private readonly string _title;

        private readonly TaskCollection _userTasks;

        private DateTime _completed = DateTime.MinValue;

        private string _finalBody = string.Empty;

        private TextToImageTaskQueueResult _genTaskResult;

        private DateTime _lastChange = DateTime.MinValue;

        private GenerationPlaceholder _placeholder;

        private QueuedTask? _queuedTask;

        private readonly TextToImageRequest _settings;

        private readonly ILogger _logger;
        readonly ChannelConfiguration _channelConfiguration;

        public SingleGenerationTask(ILogger logger, DreamCommand dreamCommand, IDiscordService? discordService, Configuration? configuration, TaskCollection? userTasks, IReadOnlyDictionary<string, IAutomaticService>? automaticServices)
        {
            _logger = Ensure.NotNull(logger);
            _discordService = Ensure.NotNull(discordService);
            _configuration = Ensure.NotNull(configuration);
            _userTasks = Ensure.NotNull(userTasks);
            _automaticServices = Ensure.NotNull(automaticServices);
            _dreamCommand = Ensure.NotNull(dreamCommand);
            _requesterId = Ensure.NotNullOrDefault(dreamCommand.User?.Id);
            _channelId = Ensure.NotNullOrDefault(dreamCommand.Channel?.Id);
            _title = $"*<@{_requesterId}> is dreaming of **{string.Join(", ", _dreamCommand.Prompt)}***";
            _channelConfiguration = ConfigurationService.GetChannelConfiguration(_channelId);

            List<string> prompt = _dreamCommand.Prompt;

            List<string> neg_prompt = _dreamCommand.NegativePrompt;

            foreach (string lora in _dreamCommand.Lora)
            {
                prompt.Insert(0, $"<lora:{lora}:{_dreamCommand.LoraStrength}> ");
            }

            if (_dreamCommand.ApplyDefaultStyles)
            {
                prompt = prompt.ApplyTemplate(_channelConfiguration.Prompt);
                neg_prompt = neg_prompt.ApplyTemplate(_channelConfiguration.NegativePrompt);
            }

            Resolution resolution = _channelConfiguration.Resolutions[_dreamCommand.AspectRatio.ToString()];

            _settings = new()
            {
                Prompt = string.Join(",", prompt.Distinct()),
                NegativePrompt = string.Join(",", neg_prompt.Distinct()),
                Width = resolution.Width,
                Height = resolution.Height,
                Seed = _dreamCommand.Seed
            };
        }

        public DateTime CreatedAt => _dreamCommand.Command.CreatedAt.DateTime;

        public ulong GuildId => _dreamCommand.Command.GuildId ?? 0;

        public int Height => _settings.Height;

        public string ImageResult => _genTaskResult.AutomaticTask?.State?.CurrentImage ?? string.Empty;

        public bool IsSuccess { get; private set; }

        public Guid LastImageName { get; private set; } = Guid.Empty;

        public Prompt NegativePrompt => new(_settings.NegativePrompt);

        public IMessage PlaceHolderMessage => _placeholder.Message;

        public Prompt Prompt => new(_settings.Prompt);

        public int QueuePosition => _genTaskResult?.QueuePosition ?? -1;

        public string SamplerName => _genTaskResult.AutomaticTask?.Request?.SamplerName ?? string.Empty;

        public long Seed => _settings.Seed;

        public int Steps => _settings.Steps;

        public IUser User => _dreamCommand.User!;

        public int Width => _settings.Width;

        public async Task GenerateImage()
        {
            _placeholder = new(await _dreamCommand.Command.FollowupAsync(_title));

            _queuedTask.MessageId = _placeholder.Message.Id;

            //Add emoji after delete event is wired up for safety and laziness;
            await _placeholder.Message.AddReactionsAsync(
            [
                Emojis.TRASH,
                ]);

            try
            {
                _genTaskResult.AutomaticTask.ProgressUpdated = this.ProgressUpdated;

                _genTaskResult.AutomaticTask.Wait();

                if (!_genTaskResult.Cancelled && string.IsNullOrWhiteSpace(_genTaskResult.AutomaticTask.State.Exception))
                {
                    if (_dreamCommand.Channel is SocketForumChannel)
                    {
                        ForumPostService forumPostService = new(_dreamCommand.Channel);

                        if (await forumPostService.IsCreator(_discordService.User.Id))
                        {
                            await forumPostService.UpdateImage(_genTaskResult.AutomaticTask!.State.CurrentImage!);
                        }
                    }

                    IsSuccess = true;
                    OnCompleted?.Invoke(this, this);
                }
            }
            finally
            {
                _userTasks.Remove(_queuedTask);
            }
        }

        public async Task ProgressUpdated(TextToImageProgress progress)
        {
            _logger.LogDebug($"Progress Updated [{LastImageName}]");

            try
            {
                if (!string.IsNullOrWhiteSpace(progress.Exception))
                {
                    _finalBody = progress.Exception;
                    _ = await _placeholder.TryUpdate(_title, _finalBody, progress.CurrentImage);
                }
                else if (progress.Completed)
                {
                    _logger.LogDebug("Progress.Completed");

                    _settings.Seed = progress.Info!.Seed;

                    _completed = DateTime.Now;
                    string mention = $"👤 <@{_requesterId}>";
                    _finalBody = _settings.ToDiscordString(_completed - _genTaskResult.QueueTime);

                    Guid nextGuid = await _placeholder.TryUpdate(string.Empty, mention + _finalBody, progress.CurrentImage);

                    if(nextGuid != Guid.Empty)
                    {
						_logger.LogDebug($"FileName: {nextGuid}");
						LastImageName = nextGuid;
                    }
                }
                else
                {
                    if (LastImageName != Guid.Empty && _lastChange.AddMilliseconds(_configuration.UpdateTimeoutMs) > DateTime.Now)
                    {
						_logger.LogDebug($"Recently updated, skipping.");
						return;
                    }

                    string displayProgress = $"{(int)(progress.Progress * 100)}% - ETA: {(int)progress.EtaRelative} seconds";

                    Guid nextGuid = await _placeholder.TryUpdate(_title, displayProgress, progress.CurrentImage);

                    _logger.LogDebug($"FileName: {nextGuid}");

                    if (nextGuid != Guid.Empty)
                    {
                        _lastChange = DateTime.Now;
                        LastImageName = nextGuid;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public bool TryQueue(out string? errorMessage)
        {
            if (string.IsNullOrWhiteSpace(_channelConfiguration.DefaultStyle))
            {
                errorMessage = "Channel does not have Default Style set";
                return false;
            }

            AutomaticEndPoint? matchingEndpoint = _configuration.Endpoints.Where(e => e.SupportedStyleNames.Contains(_channelConfiguration.DefaultStyle)).FirstOrDefault();

            if (matchingEndpoint is null)
            {
                errorMessage = $"No endpoint supporting model '{_channelConfiguration.DefaultStyle}' found in configuration";
                return false;
            }

            if (!_automaticServices.TryGetValue(matchingEndpoint.DisplayName, out IAutomaticService? automaticService))
            {
                errorMessage = $"No AutomaticService found with display name '{matchingEndpoint.DisplayName}'";
                return false;
            }

            CancellationTokenSource cts = new();

            if (!_userTasks.TryReserve(_requesterId, out QueuedTask? queuedTask))
            {
                errorMessage = $"You have already reached the max queue tasks of {_configuration.MaxUserQueue}. Try again later.";
                return false;
            }
            else
            {
                _queuedTask = queuedTask;
            }

            queuedTask.OnCancelled += (s, e) => cts.Cancel();

            _genTaskResult = automaticService.Txt2Image(_settings, cts.Token);

            queuedTask.AutomaticTask = _genTaskResult.AutomaticTask;

            errorMessage = null;
            return true;
        }
    }
}