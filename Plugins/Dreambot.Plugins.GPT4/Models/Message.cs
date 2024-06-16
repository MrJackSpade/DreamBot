using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DreamBot.Plugins.GPT4.Models
{
	public class Message
	{
		public Message()
		{
		}

		public Message(string role, string text)
		{
			Role = role;
			Content = text;
		}

		[JsonPropertyName("role")]
		public string Role { get; set; }

		[JsonPropertyName("content")]
		public string Content { get; set; }
	}
}