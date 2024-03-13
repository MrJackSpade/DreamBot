using DreamBot.Models;
using Loxifi;

namespace DreamBot.Services
{
    internal static class ConfigurationService
    {
        private static readonly object _lock = new();

        static ConfigurationService()
        {
            if (!Directory.Exists("Configurations"))
            {
                Directory.CreateDirectory("Configurations");
            }
        }

        public static ChannelConfiguration GetChannelConfiguration(ulong channelId)
        {
            lock (_lock)
            {
                return StaticConfiguration.Load<ChannelConfiguration>($"Configurations\\{channelId}.json");
            }
        }

        public static void SaveChannelConfiguration(ulong channelId, ChannelConfiguration channelConfiguration)
        {
            lock (_lock)
            {
                StaticConfiguration.Save(channelConfiguration, $"Configurations\\{channelId}.json");
            }
        }
    }
}