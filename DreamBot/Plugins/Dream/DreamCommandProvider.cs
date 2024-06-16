using Discord;
using Dreambot.Plugins.Interfaces;
using DreamBot.Collections;
using DreamBot.Constants;
using DreamBot.Models.Events;
using DreamBot.Plugins.EventArgs;
using DreamBot.Services;
using DreamBot.Shared.Interfaces;
using DreamBot.Shared.Models;
using DreamBot.Shared.Utils;

namespace DreamBot.Plugins.Dream
{
	internal class DreamCommandProvider : ICommandProvider<DreamCommand>, IReactionHandler
	{
		private readonly ThreadService _threadService = new();

		private readonly Dictionary<string, IAutomaticService> _automaticServices = new();

		private Configuration? _configuration;

		private IDiscordService? _discordService;

		private IPluginService? _pluginService;

		private TaskCollection? _userTasks;

		public string Command => "Dream";

		public string Description => "Generates an image";

		public string[] HandledReactions => [Emojis.STR_TRASH];

		public SlashCommandOption[] SlashCommandOptions => Array.Empty<SlashCommandOption>();

		public async Task<CommandResult> OnCommand(DreamCommand generateImageCommand)
		{
			SingleGenerationTask singleGenerationTask = new(generateImageCommand, _discordService, _configuration, _userTasks, _automaticServices);

			if (!singleGenerationTask.TryQueue(out string? errorMessage))
			{
				return CommandResult.Error(errorMessage!);
			}

			try
			{
				await this.SendPreGenerationEvent(singleGenerationTask, generateImageCommand.Channel);
			} catch (Exception ex)
			{
				return CommandResult.Error(ex.Message);
			}

			singleGenerationTask.OnCompleted += this.SendPostGenerationEvent;

			_threadService.Enqueue(singleGenerationTask.GenerateImage);

			int position = singleGenerationTask.QueuePosition;

			if (position > 1)
			{
				return CommandResult.Error($"Generation queued, position [{position}]");
			}
			else
			{
				return CommandResult.Success();
			}
		}

		public Task OnInitialize(InitializationEventArgs args)
		{
			_configuration = args.LoadConfiguration<Configuration>();

			_userTasks = new TaskCollection(_configuration.MaxUserQueue);

			_pluginService = args.PluginService;

			_discordService = args.DiscordService;

			foreach (AutomaticEndPoint endpoint in _configuration.Endpoints)
			{
				AutomaticService _automaticService = new (new(endpoint.AutomaticHost, endpoint.AutomaticPort)
				{
					AggressiveOptimizations = endpoint.AggressiveOptimizations
				});

				_automaticServices.Add(endpoint.DisplayName, _automaticService);
			}

			return Task.CompletedTask;
		}

		public async Task OnReaction(ReactionEventArgs args)
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
					await this.TryDeleteMessage(args);
					break;
			}
		}

		private async Task SendPreGenerationEvent(SingleGenerationTask sgt, IChannel channel)
		{
			PreGenerationEventArgs postGenerationEventArgs = new()
			{
				DateCreated = sgt.CreatedAt,
				Channel = channel,
				Guild = await _discordService.GetGuildAsync(sgt.GuildId),
				User = sgt.User,
				GenerationParameters = new GenerationParameters()
				{
					Height = sgt.Height,
					Width = sgt.Width,
					Seed = sgt.Seed,
					Prompt = sgt.Prompt,
					NegativePrompt = sgt.NegativePrompt,
					SamplerName = sgt.SamplerName,
					Steps = sgt.Steps
				}
			};

			await _pluginService.PreGenerationEvent(postGenerationEventArgs);
		}

		private async void SendPostGenerationEvent(object s, SingleGenerationTask sgt)
		{
			PostGenerationEventArgs postGenerationEventArgs = new()
			{
				Message = sgt.PlaceHolderMessage,
				DateCreated = sgt.CreatedAt,
				Images =
				[
					new GeneratedImage(sgt.LastImageName, sgt.ImageResult)
				],
				Guild = await _discordService.GetGuildAsync(sgt.GuildId),
				User = sgt.User,
				GenerationParameters = new GenerationParameters()
				{
					Height = sgt.Height,
					Width = sgt.Width,
					Seed = sgt.Seed,
					Prompt = sgt.Prompt,
					NegativePrompt = sgt.NegativePrompt,
					SamplerName = sgt.SamplerName,
					Steps = sgt.Steps
				}
			};

			await _pluginService.PostGenerationEvent(postGenerationEventArgs);
		}

		private async Task TryDeleteMessage(ReactionEventArgs args)
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