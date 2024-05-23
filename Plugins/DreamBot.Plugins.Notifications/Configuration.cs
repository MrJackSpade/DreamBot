using System.Text.Json.Serialization;

namespace DreamBot.Plugins.Notifications
{
    public class Configuration
    {
        [JsonPropertyName("excluded_roles")]
        public string[] ExcludedRoles { get; set; } = [];

        [JsonPropertyName("notification_channel_id")]
        public ulong NotificationChannelId { get; set; } = 0;

        [JsonPropertyName("notification_triggers")]
        public string[] NotificationTriggers { get; set; } = [];
    }
}