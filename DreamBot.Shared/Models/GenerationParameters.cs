namespace DreamBot.Shared.Models
{
    public class GenerationParameters
    {
        public int Height { get; init; }

        public Prompt NegativePrompt { get; init; }

        public Prompt Prompt { get; init; }

        public string SamplerName { get; init; }

        public long Seed { get; init; }

        public int Steps { get; init; }

        public int Width { get; init; }
    }
}