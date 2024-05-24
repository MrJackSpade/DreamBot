using Newtonsoft.Json;

namespace DreamBot.Models.Automatic
{
    public class TextToImageResponse
    {
        [JsonProperty("images")]
        public string[] Images { get; set; } = [];

        [JsonProperty("info")]
        public string Info { get; set; } = "";
    }
}