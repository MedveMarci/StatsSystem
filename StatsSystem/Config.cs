using System.Collections.Generic;
using System.ComponentModel;

namespace StatsSystem;

internal class Config
{
    [Description("Enable or disable the debug mode. When enabled, additional debug information will be logged.")]
    public bool Debug { get; set; } = false;

    [Description(
        "Enable or disable the playtime tracking feature. When enabled, player playtime will be tracked and saved.")]
    public bool PlaytimeTracking { get; set; } = true;

    [Description("Enable or disable the kills tracking feature. When enabled, player kills will be tracked and saved.")]
    public bool KillsTracking { get; set; } = true;

    [Description(
        "Enable or disable the deaths tracking feature. When enabled, player deaths will be tracked and saved.")]
    public bool DeathsTracking { get; set; } = true;

    [Description(
        "Enable or disable the Class D kills tracking feature. When enabled, kills made by Class D players will be tracked and saved.")]
    public bool KillsAsClassDTracking { get; set; } = true;

    [Description(
        "Enable or disable the SCP kills tracking feature. When enabled, kills made by SCPs will be tracked and saved.")]
    public bool ClassDKillsTracking { get; set; } = true;

    [Description(
        "Enable or disable the SCP kills tracking feature. When enabled, kills made by SCPs will be tracked and saved.")]
    public bool ScpKillsTracking { get; set; } = true;

    [Description(
        "Enable or disable the MicroHID kills tracking feature. When enabled, kills made by MicroHID will be tracked and saved.")]
    public bool MicroHidKillsTracking { get; set; } = true;

    [Description(
        "You can set here the days for which the 'last days' statistics will be tracked. For example, if you set this to [7, 30], the plugin will track statistics for the last 7 days and the last 30 days.")]
    public List<int> LastDays { get; set; } = [7, 30, 90];
}