using DreamBot.Plugins.Interfaces;
using DreamBot.Models.Automatic;
using DreamBot.Services;
using DreamBot.Shared.Interfaces;
using DreamBot.Shared.Loggers;
using DreamBot.Shared.Models;
using Loxifi;
using System.Reflection;

namespace DreamBot
{
    internal class Program
    {
        private static readonly DiscordService _discordService;

        private static readonly ILogger _logger = new ConsoleLogger();

        private static readonly Dictionary<string, List<Lora>> _loras = [];

        private static readonly PluginService _pluginService;

        static Program()
        {
            _discordService = new DiscordService(new DiscordServiceSettings()
            {
                Token = Configuration.Token
            });

            _pluginService = new PluginService(_logger, _discordService);
        }

        private static Configuration Configuration
        {
            get
            {
                if(!Directory.Exists("Configurations\\DreamBot"))
                {
                    Directory.CreateDirectory("Configurations\\DreamBot");
                }

               return  StaticConfiguration.Load<Configuration>("Configurations\\DreamBot\\Config.json");
            }
        }

        private static async Task Main(string[] args)
        {
            await _discordService.Connect();
            await _pluginService.LoadPlugins();

            foreach (ICommandProvider commandProvider in _pluginService.CommandProviders)
            {
                Type parameterType = commandProvider.GetType()
                                                    .GetInterface(typeof(ICommandProvider<>).Name)!
                                                    .GetGenericArguments()[0];

                MethodInfo invocationMethod = commandProvider.GetType().GetMethod(nameof(ICommandProvider<object>.OnCommand))!;

                await _discordService.AddCommand(commandProvider.Command,
                                                 commandProvider.Description,
                                                 parameterType,
                                                 c =>
                                                 {
                                                     object result = invocationMethod.Invoke(commandProvider, [c])!;
                                                     return (Task<CommandResult>)result;
												 },
                                                 commandProvider.SlashCommandOptions);
            }

            _discordService.ReactionAdded += _pluginService.React;

            await Task.Delay(-1);
        }
    }
}