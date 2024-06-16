using System.Text.Json.Serialization;

namespace DreamBot.Plugins.GPT4.Models
{
	internal class ApiResponse
	{
		[JsonPropertyName("choices")]
		public List<Choice> Choices { get; set; } = [];

		public class Choice
		{
			[JsonPropertyName("message")]
			public required Message Message { get; set; }
		}
	}
}