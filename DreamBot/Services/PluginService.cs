using Dreambot.Plugins.Interfaces;
using DreamBot.Plugins.EventArgs;
using DreamBot.Plugins.Interfaces;
using DreamBot.Shared.Extensions;
using DreamBot.Shared.Interfaces;
using DreamBot.Shared.Models;
using System.Reflection;

namespace DreamBot.Services
{
    public class PluginService
    {
        private readonly List<ICommandProvider> _commandProviders = [];

        private readonly IDiscordService _discordService;

        private readonly ILogger _logger;

        private readonly List<IPostGenerationEventHandler> _postGenerationEventHandlers = [];

        private readonly List<IPreGenerationEventHandler> _preGenerationEventHandlers = [];

        public PluginService(ILogger logger, IDiscordService discordService)
        {
            _logger = logger;
            _discordService = discordService;
            AppDomain.CurrentDomain.AssemblyResolve += this.OnResolveAssembly;
        }

        public IReadOnlyList<ICommandProvider> CommandProviders => _commandProviders;

        public IReadOnlyList<IPostGenerationEventHandler> PostGenerationEventHandlers => _postGenerationEventHandlers;

        public IReadOnlyList<IPreGenerationEventHandler> PreGenerationEventHandlers => _preGenerationEventHandlers;

        public void CallEach<T>(IList<T> collection, Action<T> action) where T : IPlugin
        {
            foreach (T handler in collection)
            {
                try
                {
                    action(handler);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error calling '{typeof(T)}' handler '{handler.GetType()}'");
                    _logger.LogError(ex);
                }
            }
        }

        public void Command<T>(T command) where T : BaseCommand
        {
            List<ICommandProvider<T>> providers = _commandProviders.OfType<ICommandProvider<T>>().ToList();
            this.CallEach(providers, h => h.OnCommand(command));
        }

        public async Task LoadPlugins()
        {
            if (!Directory.Exists("Plugins"))
            {
                return;
            }

            foreach (FileInfo dllInfo in new DirectoryInfo("Plugins").EnumerateFiles("Dreambot.Plugins.*.dll"))
            {
                _logger.LogInfo($"Loading: {Path.GetFileName(dllInfo.FullName)}");

                Assembly assembly = Assembly.LoadFile(dllInfo.FullName);

                foreach (Type type in assembly.GetTypes())
                {
                    InitializationEventArgs initializationEventArgs = new(Path.GetFileNameWithoutExtension(dllInfo.FullName),
                                                                          _logger,
                                                                          _discordService);

                    await this.TryInitialize(_preGenerationEventHandlers, type, initializationEventArgs);
                    await this.TryInitialize(_postGenerationEventHandlers, type, initializationEventArgs);
                    await this.TryInitialize(_commandProviders, type, initializationEventArgs);
                }
            }
        }

        public void PostGenerationEvent(PostGenerationEventArgs args)
        {
            this.CallEach(_postGenerationEventHandlers, h => h.OnPostGeneration(args));
        }

        public void PreGenerationEvent(PostGenerationEventArgs args)
        {
            this.CallEach(_preGenerationEventHandlers, h => h.OnPreGeneration(args));
        }

        private Assembly? OnResolveAssembly(object sender, ResolveEventArgs args)
        {
            string assemblyName = new AssemblyName(args.Name).Name + ".dll";

            var assemblyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", assemblyName);

            if (File.Exists(assemblyPath))
            {
                _logger.LogDebug($"Loading: {assemblyName}");
                return Assembly.LoadFile(assemblyPath); 
            }

            return null;
        }

        public async Task TryInitialize<T>(IList<T> collection, Type t, InitializationEventArgs initializationEventArgs) where T : IPlugin
        {
            if (t.GetInterface(typeof(T).Name) != null && !t.ContainsGenericParameters)
            {
                object? instance;

                try
                {
                    instance = Activator.CreateInstance(t);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error constructing plugin '{typeof(T)}'");
                    _logger.LogError(ex);
                    return;
                }

                if (instance is T plugin)
                {
                    try
                    {
                        await plugin.OnInitialize(initializationEventArgs);
                        collection.Add(plugin);

                        _logger.LogInfo($"Initialized '{typeof(T)}' handler '{t}'");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error initializing '{typeof(T)}' handler '{t}'");
                        _logger.LogError(ex);
                    }
                }
            }
        }
    }
}