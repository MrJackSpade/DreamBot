using Dreambot.Plugins.EventResults;
using Dreambot.Plugins.Interfaces;
using DreamBot.Models.Events;
using DreamBot.Plugins.EventArgs;
using DreamBot.Plugins.Interfaces;
using DreamBot.Shared.Extensions;
using DreamBot.Shared.Interfaces;
using DreamBot.Shared.Models;
using System.Reflection;

namespace DreamBot.Services
{
    public class PluginService : IPluginService
    {
        private readonly Dictionary<Type, object> _cache = [];

        private readonly List<ICommandProvider> _commandProviders = [];

        private readonly IDiscordService _discordService;

        private readonly ILogger _logger;

        private readonly List<IPostGenerationEventHandler> _postGenerationEventHandlers = [];

        private readonly List<IPreGenerationEventHandler> _preGenerationEventHandlers = [];

        private readonly List<IReactionHandler> _reactionHandlers = [];

        public PluginService(ILogger logger, IDiscordService discordService)
        {
            _logger = logger;
            _discordService = discordService;
            AppDomain.CurrentDomain.AssemblyResolve += this.OnResolveAssembly!;
        }

        public IReadOnlyList<ICommandProvider> CommandProviders => _commandProviders;

        public IReadOnlyList<IPostGenerationEventHandler> PostGenerationEventHandlers => _postGenerationEventHandlers;

        public IReadOnlyList<IPreGenerationEventHandler> PreGenerationEventHandlers => _preGenerationEventHandlers;

        public IReadOnlyList<IReactionHandler> ReactionHandlers => _reactionHandlers;

		public async Task CallEach<T>(IList<T> collection, Func<T, Task> action) where T : IPlugin
		{
			foreach (T handler in collection)
			{
				try
				{
					await action(handler);
				}
				catch (Exception ex)
				{
					_logger.LogError($"Error calling '{typeof(T).Name}' handler '{handler.GetType()}'");
					_logger.LogError(ex);
				}
			}
		}

		public async Task Command<T>(T command) where T : BaseCommand
        {
            List<ICommandProvider<T>> providers = _commandProviders.OfType<ICommandProvider<T>>().ToList();
            await this.CallEach(providers, h => h.OnCommand(command));
        }

        public async Task LoadPlugins(Assembly assembly, string assemblyName)
        {
            foreach (Type type in assembly.GetTypes())
            {
                InitializationEventArgs initializationEventArgs = new(Path.GetFileNameWithoutExtension(assemblyName),
                                                                      this,
                                                                      _logger,
                                                                      _discordService);

                await this.TryInitialize(_preGenerationEventHandlers, type, initializationEventArgs);
                await this.TryInitialize(_postGenerationEventHandlers, type, initializationEventArgs);
                await this.TryInitialize(_commandProviders, type, initializationEventArgs);
                await this.TryInitialize(_reactionHandlers, type, initializationEventArgs);
            }
        }

        public async Task LoadPlugins()
        {
            if (Directory.Exists("Plugins"))
            {
                foreach (FileInfo dllInfo in new DirectoryInfo("Plugins").EnumerateFiles("Dreambot.Plugins.*.dll"))
                {
                    _logger.LogInfo($"Loading: {Path.GetFileName(dllInfo.FullName)}");

                    Assembly assembly = Assembly.LoadFile(dllInfo.FullName);

                    await this.LoadPlugins(assembly, dllInfo.FullName);
                }
            }

            await this.LoadPlugins(Assembly.GetEntryAssembly()!, "DreamBot");
        }

        public async Task PostGenerationEvent(PostGenerationEventArgs args)
        {
            await this.CallEach(_postGenerationEventHandlers, h => h.OnPostGeneration(args));
        }

        public async Task PreGenerationEvent(PreGenerationEventArgs args)
        {
            await this.CallEach(_preGenerationEventHandlers, h => h.OnPreGeneration(args));
        }

        public async Task React(ReactionEventArgs args)
        {
            foreach (IReactionHandler reactionHandler in this._reactionHandlers)
            {
                if (reactionHandler.HandledReactions.Contains(args.SocketReaction.Emote.Name))
                {
                    try
                    {
                        await reactionHandler.OnReaction(args);

                        _logger.LogInfo($"Reacted '{args.SocketReaction.Emote.Name}' handler '{reactionHandler.GetType()}'");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error initializing '{args.SocketReaction.Emote.Name}' handler '{reactionHandler.GetType()}'");
                        _logger.LogError(ex);
                        return;
                    }
                }
            }
        }

        public async Task TryInitialize<T>(IList<T> collection, Type t, InitializationEventArgs initializationEventArgs) where T : IPlugin
        {
            if (t.GetInterface(typeof(T).Name) != null && !t.ContainsGenericParameters)
            {
                if (_cache.TryGetValue(t, out object? instance))
                {
                    if (instance is T plugin)
                    {
                        collection.Add(plugin);

                        _logger.LogInfo($"Used existing '{typeof(T).Name}' handler '{t}'");
                    }
                    else
                    {
                        _logger.LogError($"Found existing '{typeof(T).Name}' handler '{t}' but the type is incorrect?");
                    }
                }
                else
                {
                    try
                    {
                        instance = Activator.CreateInstance(t);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error constructing plugin '{typeof(T).Name}'");
                        _logger.LogError(ex);
                        return;
                    }

                    if (instance is T plugin)
                    {
                        try
                        {
                            InitializationResult result = await plugin.OnInitialize(initializationEventArgs);

                            if (result.IsSuccess)
                            {
                                collection.Add(plugin);

                                _logger.LogInfo($"Initialized '{typeof(T).Name}' handler '{t}'");
                            }

                            if (result.IsCancel)
                            {
                                _logger.LogWarn($"Cancelled initialization of '{typeof(T).Name}' handler '{t}'");
							}
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error initializing '{typeof(T).Name}' handler '{t}'");
                            _logger.LogError(ex);
                            return;
                        }
                    }
                }
            }
        }

        private Assembly? OnResolveAssembly(object sender, ResolveEventArgs args)
        {
            string assemblyName = new AssemblyName(args.Name).Name + ".dll";

            string assemblyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", assemblyName);

            if (File.Exists(assemblyPath))
            {
                _logger.LogDebug($"Loading: {assemblyName}");
                return Assembly.LoadFile(assemblyPath);
            }

            return null;
        }
    }
}