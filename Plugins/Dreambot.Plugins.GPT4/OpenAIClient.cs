using DreamBot.Plugins.GPT4.Models;
using System.Text;
using System.Text.Json;

namespace DreamBot.Plugins.GPT4
{
    public class OpenAIClient
	{
		public OpenAIClient(string apiKey, string systemPrompt)
		{
			_apiKey = apiKey;

			if (!string.IsNullOrEmpty(systemPrompt))
			{
				_messages.Add(new Message("system", systemPrompt));
			}
		}

		private string _apiKey { get; set; }

		private List<Message> _messages { get; set; } = new List<Message>();

		public async Task<Message> GetResponseAsync(string input)
		{
			_messages.Add(new Message("user", input));

			string responseMessage = await this.MakeApiCallAsync(_messages);

			ApiResponse? response = JsonSerializer.Deserialize<ApiResponse>(responseMessage);

			string botMessage = response.Choices.First().Message.Content;

			Message toReturn = new("assistant", botMessage);

			_messages.Add(toReturn);

			return toReturn;
		}

		private async Task<string> MakeApiCallAsync(List<Message> messages)
		{
			using var client = new HttpClient();

			client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

			var requestBody = new
			{
				model = "gpt-4o",
				messages = messages,
				temperature = 0,
				max_tokens = 256,
				top_p = 1,
				frequency_penalty = 0,
				presence_penalty = 0
			};

			var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

			var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);

			var responseText = await response.Content.ReadAsStringAsync();

			response.EnsureSuccessStatusCode();

			return await response.Content.ReadAsStringAsync();
		}
	}
}