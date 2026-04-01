using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LabApi.Events.CustomHandlers;
using LabApi.Features;
using LabApi.Loader.Features.Paths;
using LabApi.Loader.Features.Plugins;
using MEC;
using StatsSystem.API;
using StatsSystem.ApiFeatures;
using StatsSystem.Storage;
using EventHandler = StatsSystem.Events.EventHandler;
using Version = System.Version;

namespace StatsSystem;

internal sealed class StatsSystemPlugin : Plugin<Config>
{
    private static IStorageProvider _activeProvider;
    
    private CoroutineHandle _autoSaveHandle;
    private EventHandler _eventHandler;
    private string _statsDirectory;
    
    public override string Name => "StatsSystem";
    public override string Description => "Professional player-statistics tracking for SCP:SL servers.";
    public override string Author => "MedveMarci";
    public override Version Version { get; } = new(2, 0, 0);
    public override Version RequiredApiVersion => new(LabApiProperties.CompiledVersion);
    public override bool IsTransparent => true;

    public static StatsSystemPlugin Singleton { get; private set; }

    public static IStatsProvider Stats { get; private set; }

    public static void UseStorageProvider(IStorageProvider provider)
    {
        if (provider == null) throw new ArgumentNullException(nameof(provider));
        if (Stats is not StatsRepository repo) return;

        Stats.Save();
        _activeProvider?.Dispose();
        _activeProvider = provider;
        repo.SetStorageProvider(provider);
        Stats.Reload();
        LogManager.Info($"Storage provider switched to {provider.GetType().Name}.");
    }

    public override void Enable()
    {
        Singleton = this;

        _statsDirectory = Path.Combine(PathManager.Configs.FullName, Config.StatsDataFolder);
        if (!Directory.Exists(_statsDirectory))
        {
            LogManager.Warn($"Stats directory not found — creating '{_statsDirectory}'.");
            Directory.CreateDirectory(_statsDirectory);
        }

        _activeProvider = CreateProvider();
        var defaultId = "player_stats";

        if (Config.StorageProvider == StorageProviderType.Json)
        {
            var jsonPath = Path.Combine(_statsDirectory, "player_stats.json");
            if (!File.Exists(jsonPath)) File.WriteAllText(jsonPath, "{}");
        }

        LogManager.Info($"Stats stored at '{_statsDirectory}' using {Config.StorageProvider} provider.");

        Stats = new StatsRepository(defaultId, _activeProvider);

        _eventHandler = new EventHandler();
        Shutdown.OnQuit += EventHandler.OnQuit;
        CustomHandlersManager.RegisterEventsHandler(_eventHandler);

        var interval = Math.Max(10, Config.AutoSaveIntervalSeconds);
        _autoSaveHandle = Timing.RunCoroutine(AutoSaveCoroutine(interval));

        LogManager.Info($"StatsSystem v{Version} enabled. Auto-save every {interval}s.");
    }

    public override void Disable()
    {
        Timing.KillCoroutines(_autoSaveHandle);
        Shutdown.OnQuit -= EventHandler.OnQuit;
        CustomHandlersManager.UnregisterEventsHandler(_eventHandler);

        Stats?.Save();
        LogManager.Info("Stats saved. StatsSystem disabled.");

        _activeProvider?.Dispose();
        _activeProvider = null;
        Stats = null;
        Singleton = null;
    }

    private IStorageProvider CreateProvider()
    {
        return Config.StorageProvider switch
        {
            StorageProviderType.Binary => new BinaryStorageProvider(_statsDirectory),
            _ => new JsonStorageProvider(_statsDirectory)
        };
    }

    private static IEnumerator<float> AutoSaveCoroutine(int intervalSeconds)
    {
        while (true)
        {
            yield return Timing.WaitForSeconds(intervalSeconds);
            if (Stats == null) yield break;
            Task.Run(() => Stats.Save());
            LogManager.Debug("Auto-save triggered.");
        }
    }
}
