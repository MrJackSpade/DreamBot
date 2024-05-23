using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DreamBot.Plugins.Honeypot
{
    public class Configuration
    {
        [JsonPropertyName("notification_channel_id")]
        public ulong NotificationChannelId { get; set; } = 0;
    }
}
