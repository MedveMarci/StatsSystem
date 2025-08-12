using System;
using System.IO;
using System.Threading.Tasks;
using LabApi.Events.CustomHandlers;
using LabApi.Loader.Features.Paths;
using LabApi.Loader.Features.Plugins;
using StatsSystem.Managers;
using Version = System.Version;

namespace StatsSystem;

internal class StatsSystemPlugin : Plugin<Config>
{
    public override string Name => "StatsSystem";

    public override string Description => "StatSystem";

    public override string Author => "MedveMarci";

    public override Version Version { get; } = new(2025, 8, 12, 2);

    public override Version RequiredApiVersion => new(1, 1, 1);

    internal static StatsSystemPlugin Instance;
    
    internal static API.StatsSystem StatsSystem;

    private static Events.EventHandler _eventHandler;

    private static string _saveFilePath;

    private static string _saveFileDirectory;

    public override void Enable()
    {
        Instance = this;
        _eventHandler = new Events.EventHandler();
        
        Shutdown.OnQuit += Events.EventHandler.OnQuit;
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
        StatsSystem.LoadStats();
        Task.Run(StartPeriodicSaving);
    }

    public override void Disable()
    {
        Shutdown.OnQuit -= Events.EventHandler.OnQuit;
        CustomHandlersManager.UnregisterEventsHandler(_eventHandler);
        StatsSystem.SaveStats();
        LogManager.Info("Player stats saved to: " + _saveFilePath);
        StatsSystem = null;
        Instance = null;
    }
    
    private static async Task StartPeriodicSaving()
    {
        while (ServerShutdown.ShutdownState == ServerShutdown.ServerShutdownState.NotInitiated)
        {
            await Task.Delay(TimeSpan.FromMinutes(1));
                
            await StatsSystem.SaveStatsAsync();
            LogManager.Debug("Player stats saved automatically.");
            
        }
    }
}