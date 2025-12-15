using System;
using System.IO;
using System.Threading.Tasks;
using LabApi.Events.CustomHandlers;
using LabApi.Features;
using LabApi.Loader.Features.Paths;
using LabApi.Loader.Features.Plugins;
using StatsSystem.Managers;
using EventHandler = StatsSystem.Events.EventHandler;
using Version = System.Version;

namespace StatsSystem;

internal class StatsSystemPlugin : Plugin<Config>
{
    internal static StatsSystemPlugin Singleton;
    internal static API.StatsSystem StatsSystem;
    private static EventHandler _eventHandler;
    private static string _saveFilePath;
    private static string _saveFileDirectory;
    public override string Name => "StatsSystem";
    public override string Description => "StatSystem";
    public override string Author => "MedveMarci";
    public override Version Version { get; } = new(1, 1, 5);
    public override Version RequiredApiVersion => new(LabApiProperties.CompiledVersion);
    public override bool IsTransparent => true;
    public string githubRepo = "MedveMarci/StatsSystem";

    public override void Enable()
    {
        Singleton = this;
        _eventHandler = new EventHandler();

        Shutdown.OnQuit += EventHandler.OnQuit;
        CustomHandlersManager.RegisterEventsHandler(_eventHandler);

        _saveFileDirectory = Path.Combine(PathManager.Configs.FullName, Name);
        if (!Directory.Exists(_saveFileDirectory))
        {
            LogManager.Warn($"{Name} directory does not exist. Creating...");
            Directory.CreateDirectory(_saveFileDirectory);
        }

        _saveFilePath = Path.Combine(_saveFileDirectory, "player_stats.json");
        if (!File.Exists(_saveFilePath))
        {
            LogManager.Warn("Player stats file does not exist. Creating...");
            File.Create(_saveFilePath).Close();
        }

        LogManager.Info("Player stats will be saved to: " + _saveFilePath);
        StatsSystem = new API.StatsSystem(_saveFilePath);
        Task.Run(StartPeriodicSaving);
    }

    public override void Disable()
    {
        Shutdown.OnQuit -= EventHandler.OnQuit;
        CustomHandlersManager.UnregisterEventsHandler(_eventHandler);
        StatsSystem.SaveStats();
        LogManager.Info("Player stats saved to: " + _saveFilePath);
        StatsSystem = null;
        Singleton = null;
    }

    private static async Task StartPeriodicSaving()
    {
        while (ServerShutdown.ShutdownState == ServerShutdown.ServerShutdownState.NotInitiated)
        {
            LogManager.Debug("Player stats saved automatically.");
            await StatsSystem.SaveStatsAsync();
            await Task.Delay(TimeSpan.FromMinutes(1));
        }
    }
}