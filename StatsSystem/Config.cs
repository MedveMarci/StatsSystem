using System.ComponentModel;

namespace StatsSystem;

internal class Config
{
    [Description("Enable or disable the debug mode. When enabled, additional debug information will be logged.")]
    public bool Debug { get; set; } = false;
}