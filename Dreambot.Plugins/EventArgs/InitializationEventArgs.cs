using DreamBot.Shared.Interfaces;

namespace DreamBot.Plugins.EventArgs
{
    public struct InitializationEventArgs
    {
        private readonly string _module;

        public InitializationEventArgs(string module, IPluginService pluginService, ILogger logger, IDiscordService discordService)
        {
            _module = module;
            Logger = logger;
            DiscordService = discordService;
            PluginService = pluginService;
        }

        public IReadOnlyDictionary<string, IAutomaticService> AutomaticServices { get; set; }

        public IDiscordService DiscordService { get; private set; }

        public ILogger Logger { get; private set; }

        public IPluginService PluginService { get; set; }

        public T LoadConfiguration<T>() where T : class, new()
        {
            string configurationDir = Directory.GetCurrentDirectory();

            configurationDir = Path.Combine(configurationDir, "Configurations");

            if (!Directory.Exists(configurationDir))
            {
                Directory.CreateDirectory(configurationDir);
            }

            configurationDir = Path.Combine(configurationDir, _module);

            if (!Directory.Exists(configurationDir))
            {
                Directory.CreateDirectory(configurationDir);
            }

            string configurationPath = Path.Combine(configurationDir, "Config.json");

            return Loxifi.StaticConfiguration.Load<T>(configurationPath);
        }
    }
}