using Dreambot.Plugins.Interfaces;
using DreamBot.Models.Automatic;
using DreamBot.Services;
using DreamBot.Shared.Interfaces;
using DreamBot.Shared.Loggers;
using Loxifi;
using System.Reflection;

namespace DreamBot
{
    internal class Program
    {
        private static readonly Dictionary<string, AutomaticService> _automaticServices = [];

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

            foreach (AutomaticEndPoint endpoint in Configuration.Endpoints)
            {
                AutomaticService _automaticService = new(new(endpoint.AutomaticHost, endpoint.AutomaticPort)
                {
                    AggressiveOptimizations = endpoint.AggressiveOptimizations
                });

                _automaticServices.Add(endpoint.DisplayName, _automaticService);
            }

            _pluginService = new PluginService(_logger, _discordService);
        }

        private static Configuration Configuration => StaticConfiguration.Load<Configuration>("Configurations\\DreamBot\\Configuration.json");

        private static async Task Main(string[] args)
        {
            await _discordService.Connect();
            await _pluginService.LoadPlugins();

            foreach (AutomaticEndPoint endpoint in Configuration.Endpoints)
            {
                AutomaticService _automaticService = _automaticServices[endpoint.DisplayName];

                Lora[] loras = [];

                try
                {
                    loras = await _automaticService.GetLoras()!;
                }
                catch (Exception ex)
                {
                    continue;
                }

                foreach (string modelStyle in endpoint.SupportedStyleNames)
                {
                    if (!_loras.TryGetValue(modelStyle, out List<Lora>? styleLoras))
                    {
                        styleLoras = [.. loras];
                        _loras[modelStyle] = styleLoras;
                    }
                    else
                    {
                        foreach (Lora l in styleLoras.ToList())
                        {
                            if (!loras.Contains(l))
                            {
                                _ = styleLoras.Remove(l);
                                Console.WriteLine($"Removing LORA {l.Name} not supported by endpoint {endpoint.DisplayName}");
                            }
                        }
                    }
                }
            }

            foreach (ICommandProvider commandProvider in _pluginService.CommandProviders)
            {
                Type parameterType = commandProvider.GetType()
                                                    .GetInterface(typeof(ICommandProvider<>).Name)!
                                                    .GetGenericArguments()[0];

                MethodInfo invocationMethod = commandProvider.GetType().GetMethod(nameof(ICommandProvider<object>.OnCommand))!;

                await _discordService.AddCommand(commandProvider.Command,
                                                 commandProvider.Description,
                                                 parameterType,
                                                 c => (Task<string>)invocationMethod.Invoke(commandProvider, [c])!,
                                                 commandProvider.SlashCommandOptions);
            }

            _discordService.ReactionAdded += _pluginService.React;

            await Task.Delay(-1);
        }
    }
}