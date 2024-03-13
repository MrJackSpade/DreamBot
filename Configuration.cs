using Newtonsoft.Json;

namespace DreamBot
{
    public class AutomaticEndPoint
    {
        [JsonProperty("aggressive_optimizations")]
        public bool AggressiveOptimizations { get; set; } = false;

        [JsonProperty("automatic_host")]
        public string? AutomaticHost { get; set; } = "127.0.0.1";

        [JsonProperty("automatic_port")]
        public int AutomaticPort { get; set; } = 7860;

        [JsonProperty("display_name")]
        public string DisplayName { get; set; } = "Default";

        [JsonProperty("supported_style_names")]
        public string[] SupportedStyleNames { get; set; } = ["Default"];
    }

    public class Configuration
    {
        [JsonProperty("endpoints")]
        public AutomaticEndPoint[] Endpoints { get; set; } = [new AutomaticEndPoint()];

        [JsonProperty("max_user_queue")]
        public int MaxUserQueue { get; set; } = 1;

        [JsonProperty("styles")]
        public Style[] Styles { get; set; } = [new Style()];

        [JsonProperty("thread_creation_channels")]
        public ulong[] ThreadCreationChannels { get; set; } = [];

        [JsonProperty("token")]
        public string? Token { get; set; }

        [JsonProperty("update_timeout_ms")]
        public int UpdateTimeoutMs { get; set; } = 3000;
    }

    public class Style
    {
        [JsonProperty("display_name")]
        public string? DisplayName { get; set; } = "Default";

        [JsonProperty("model_name")]
        public string? ModelName { get; set; }

        [JsonProperty("negative_prompt")]
        public string? NegativePrompt { get; set; }

        [JsonProperty("positive_prompt")]
        public string? PositivePrompt { get; set; }
    }
}