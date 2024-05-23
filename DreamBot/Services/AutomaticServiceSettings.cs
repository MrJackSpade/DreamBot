namespace DreamBot.Services
{
    public class AutomaticServiceSettings
    {
        public AutomaticServiceSettings(string host = "127.0.0.1", int port = 7860)
        {
            this.Host = host;
            this.Port = port;
        }

        public bool AggressiveOptimizations { get; internal set; }

        public string Host { get; set; } = "127.0.0.1";

        public int Port { get; set; } = 7860;
    }
}