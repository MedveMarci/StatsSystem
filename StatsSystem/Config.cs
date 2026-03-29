using System.Collections.Generic;
using System.ComponentModel;

namespace StatsSystem;

internal sealed class Config
{
    [Description("Enable verbose debug logging to the server console.")]
    public bool Debug { get; set; } = false;

    [Description("Track total playtime per player (stored as 'TotalPlayTime' duration).")]
    public bool PlaytimeTracking { get; set; } = true;

    [Description("Track total kills per player.")]
    public bool KillsTracking { get; set; } = true;

    [Description("Track total deaths per player.")]
    public bool DeathsTracking { get; set; } = true;

    [Description("Track kills made while the attacker is Class D.")]
    public bool KillsAsClassDTracking { get; set; } = true;

    [Description("Track kills where the victim is Class D.")]
    public bool ClassDKillsTracking { get; set; } = true;

    [Description("Track kills where the victim is an SCP.")]
    public bool ScpKillsTracking { get; set; } = true;

    [Description("Track kills made with the MicroHID.")]
    public bool MicroHidKillsTracking { get; set; } = true;

    [Description("Historical time windows (in days) shown in /getstat and /getleaderboard. E.g. [7, 30, 90].")]
    public List<int> LastDays { get; set; } = [7, 30, 90];

    [Description("Folder (relative to plugin configs path) where stats files are stored.")]
    public string StatsDataFolder { get; set; } = "StatsSystem";

    [Description("How often (in seconds) stats are automatically saved to disk. Minimum: 10.")]
    public int AutoSaveIntervalSeconds { get; set; } = 60;

    [Description(
        "Storage backend to use for persistence.\n" +
        "  Json   - Human-readable JSON files. Backwards compatible with v1 data. Default.\n" +
        "  Binary - Compact binary format (~60-80% smaller, 3-5x faster to read/write).\n" +
        "           Not human-readable. When switching from Json, run 'ss migrate' first\n" +
        "           or your existing data will not be loaded automatically.")]
    public StorageProviderType StorageProvider { get; set; } = StorageProviderType.Json;
}

public enum StorageProviderType
{
    Json,
    Binary
}