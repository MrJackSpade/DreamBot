﻿using Discord.WebSocket;
using DreamBot.Plugins.EventArgs;
using DreamBot.Plugins.GPT4.Models;
using DreamBot.Plugins.Interfaces;
using DreamBot.Shared.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DreamBot.Plugins.GPT4
{
	internal class PreGenerationEventHandler : IPreGenerationEventHandler
	{
		private Configuration? _configuration;

		private SocketTextChannel? NotificationChannel;

		private const string SystemPrompt = """
			You are a content moderation bot in an AI image generation server. To help prevent generation of illegal content, the user will give you a stable diffusion prompt, and must return your response in the following format:

			AGE: [CHILD/TEEN/ADULT/UNSPECIFIED]
			PERSONHOOD: [FICTIONAL/REAL/CELEBRITY/UNSPECIFIED]
			STATE: [SAFE/LEWD/PORN]

			It is important that you follow the response format exactly so that the API can take the required action.

			Please evaluate the users prompt
			""";

		public async Task OnInitialize(InitializationEventArgs args)
		{
			_configuration = args.LoadConfiguration<Configuration>();

			if (await args.DiscordService.GetChannelAsync(_configuration.NotificationChannelId) is SocketTextChannel stc)
			{
				NotificationChannel = stc;
			}
			else
			{
				throw new MissingChannelException(_configuration.NotificationChannelId);
			}
		}

		private ConcurrentDictionary<string, string> _cachedResults = new ConcurrentDictionary<string, string>();

		public async Task OnPreGeneration(PreGenerationEventArgs args)
		{
	
			if (_configuration?.ApiKey != null)
			{
				
				string result = null;

				string prompt = args.GenerationParameters.Prompt.Text;

				if(!_cachedResults.TryGetValue(prompt, out result))
				{
					OpenAIClient openAIClient = new(_configuration.ApiKey, SystemPrompt);

					Message response = await openAIClient.GetResponseAsync(prompt);

					result = response.Content;

					_cachedResults.TryAdd(prompt, result);
				}

				string notificationMessage = $"""
				```
				{args.GenerationParameters.Prompt.Text}
				{result}
				```
				""";

				if (NotificationChannel != null)
				{
					await NotificationChannel.SendMessageAsync(notificationMessage);
				}
			}
		}
	}
}
