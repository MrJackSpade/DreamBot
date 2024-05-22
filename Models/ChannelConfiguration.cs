namespace DreamBot.Models
{
    public class ChannelConfiguration
    {
        public string DefaultStyle { get; set; } = string.Empty;
        public string NegativePrompt { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;

        public Dictionary<string, Resolution> Resolutions { get; set; } = new Dictionary<string, Resolution>()
        {
            [nameof(AspectRatio.Landscape)] = new Resolution(1344, 768),
            [nameof(AspectRatio.Portrait)] = new Resolution(768, 1344),
            [nameof(AspectRatio.Square)] = new Resolution(1024, 1024),
        };
    }

    public class Resolution(int width, int height)
    {
        public int Height { get; set; } = height;

        public int Width { get; set; } = width;
    }
}