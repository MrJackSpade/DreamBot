using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DreamBot.Plugins.GPT4
{
	public class Configuration
	{
		[JsonPropertyName("api_key")]
		public string? ApiKey { get; set; }

		[JsonPropertyName("trusted_role")]
		public string? TrustedRole { get; set; } = string.Empty;

		[JsonPropertyName("notification_channel_id")]
		public ulong NotificationChannelId { get; set; } = 0;
	}
}
