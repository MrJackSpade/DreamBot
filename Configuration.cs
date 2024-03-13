using Newtonsoft.Json;

namespace DreamBot
{
    public class Configuration
    {
        [JsonProperty("aggressive_optimizations")]
        public bool AggressiveOptimizations { get; set; } = false;

        [JsonProperty("automatic_host")]
        public string? AutomaticHost { get; set; } = "127.0.0.1";

        [JsonProperty("automatic_port")]
        public int AutomaticPort { get; set; } = 7860;

        [JsonProperty("max_user_queue")]
        public int MaxUserQueue { get; set; } = 1;

        [JsonProperty("token")]
        public string? Token { get; set; }

        [JsonProperty("update_timeout_ms")]
        public int UpdateTimeoutMs { get; set; } = 3000;
    }
}