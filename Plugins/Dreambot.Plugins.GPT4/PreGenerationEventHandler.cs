using Discord.WebSocket;
using DreamBot.Plugins.EventArgs;
using DreamBot.Plugins.GPT4.Exceptions;
using DreamBot.Plugins.GPT4.Models;
using DreamBot.Plugins.Interfaces;
using DreamBot.Shared.Exceptions;
using System.Collections.Concurrent;
using DreamBot.Shared.Extensions;
using System.Text;
using DreamBot.Plugins.EventResults;

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
			STATE: [SAFE/LEWD/PORN/UNSPECIFIED]

			Use of the word "girl" alone should NOT be considered indicative of a child age.

			It is important that you follow the response format exactly so that the API can take the required action.

			Please evaluate the users prompt
			""";

		public async Task<InitializationResult> OnInitialize(InitializationEventArgs args)
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

			return InitializationResult.Success();
		}

		private readonly ConcurrentDictionary<string, string> _cachedResults = new();

		public async Task OnPreGeneration(PreGenerationEventArgs args)
		{
			if(_configuration.TrustedRole is string trustedRole)
			{
				if (args.User.HasRole(trustedRole))
				{
					return;
				}
			}

			if (_configuration?.ApiKey != null)
			{
				string prompt = args.GenerationParameters.Prompt.Text;

                if (!_cachedResults.TryGetValue(prompt, out string? result))
                {
                    OpenAIClient openAIClient = new(_configuration.ApiKey, SystemPrompt);

                    Message response = await openAIClient.GetResponseAsync(prompt);

                    result = response.Content;

                    _cachedResults.TryAdd(prompt, result);
                }

                string[] responseLines = result.Split('\n');

				string age = responseLines[0].Split(':')[1].Trim();
				string personhood = responseLines[1].Split(':')[1].Trim();
				string state = responseLines[2].Split(':')[1].Trim();

				bool flag = false;

				flag = flag || age == "CHILD" && state != "SAFE";
				flag = flag || (personhood == "CELEBRITY" && (state == "PORN" || state == "LEWD"));

				bool newUser = args.User is SocketGuildUser sgu && sgu.JoinedAt > DateTime.UtcNow.AddDays(-7);
                bool isBlocked = flag && newUser;

				string notificationMessage = $"""
				```
				{args.GenerationParameters.Prompt.Text}
				{result}
				Blocked: {isBlocked}
				```
				""";

				if (flag && NotificationChannel != null)
				{
					StringBuilder sb = new();
					sb.AppendLine($"https://discord.com/channels/{args.Guild.Id}/{args.Channel.Id}");
					sb.AppendLine($"<@{args.User.Id}>");
					sb.AppendLine(notificationMessage);
					await NotificationChannel.SendMessageAsync(sb.ToString());
				}

				if(isBlocked)
				{
					throw new InappropriatePromptException();
				}
			}
		}
	}
}
