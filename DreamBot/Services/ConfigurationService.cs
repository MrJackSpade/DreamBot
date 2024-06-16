using DreamBot.Models;
using Loxifi;

namespace DreamBot.Services
{
	internal static class ConfigurationService
	{
		private const string CHANNEL_CONFIG_PATH = "Configurations\\DreamBot\\Channels";

		private static readonly object _lock = new();

		static ConfigurationService()
		{
			if (!Directory.Exists(CHANNEL_CONFIG_PATH))
			{
				Directory.CreateDirectory(CHANNEL_CONFIG_PATH);
			}
		}

		public static ChannelConfiguration GetChannelConfiguration(ulong channelId)
		{
			lock (_lock)
			{
				return StaticConfiguration.Load<ChannelConfiguration>($"{CHANNEL_CONFIG_PATH}\\{channelId}.json");
			}
		}

		public static IEnumerable<ulong> GetConfiguredChannels()
		{
			foreach (string file in Directory.EnumerateFiles("Configurations"))
			{
				if (ulong.TryParse(Path.GetFileNameWithoutExtension(file), out ulong cid))
				{
					yield return cid;
				}
			}
		}

		public static void SaveChannelConfiguration(ulong channelId, ChannelConfiguration channelConfiguration)
		{
			lock (_lock)
			{
				StaticConfiguration.Save(channelConfiguration, $"{CHANNEL_CONFIG_PATH}\\{channelId}.json");
			}
		}
	}
}