using Newtonsoft.Json;

namespace DreamBot.Models.Automatic
{
    public class Txt2ImgResponse
    {
        [JsonProperty("images")]
        public string[] Images { get; set; } = [];

        [JsonProperty("info")]
        public string Info { get; set; } = "";
    }

    // The Txt2ImgParameters class remains the same as before
}