namespace DreamBot.Services
{
    public class DiscordServiceSettings
    {
        public int MaxSenderCache { get; set; } = 10_000;

        public string Token { get; set; }
    }
}