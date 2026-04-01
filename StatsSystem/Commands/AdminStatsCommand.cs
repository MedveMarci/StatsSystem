using System;
using System.Linq;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;
using StatsSystem.Migration;

namespace StatsSystem.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(GameConsoleCommandHandler))]
public sealed class AdminStatsCommand : ParentCommand
{
    public AdminStatsCommand()
    {
        LoadGeneratedCommands();
    }

    public override string Command => "ss";
    public override string[] Aliases { get; } = ["statsadmin"];
    public override string Description => "StatsSystem admin commands. Run 'ss help' for subcommands.";

    public override void LoadGeneratedCommands()
    {
        RegisterCommand(new SaveSubcommand());
        RegisterCommand(new ReloadSubcommand());
        RegisterCommand(new ResetPlayerSubcommand());
        RegisterCommand(new MigrateSubcommand());
        RegisterCommand(new InfoSubcommand());
    }

    protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        response = "Usage: ss <save | reload | resetplayer | migrate | info>";
        return false;
    }

    internal static bool AdminCheck(ICommandSender sender, out string response)
    {
        var player = Player.Get(sender);
        if (player == null)
        {
            response = null;
            return true;
        }

        if (!player.HasPermissions("stat.manage"))
        {
            response = "Missing permission: stat.manage";
            return false;
        }

        response = null;
        return true;
    }
}

internal sealed class SaveSubcommand : ICommand
{
    public string Command => "save";
    public string[] Aliases => ["s"];
    public string Description => "Immediately saves all stats to disk.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!AdminStatsCommand.AdminCheck(sender, out response)) return false;
        StatsSystemPlugin.Stats.Save();
        response = "Stats saved to disk.";
        return true;
    }
}

internal sealed class ReloadSubcommand : ICommand
{
    public string Command => "reload";
    public string[] Aliases => ["r"];
    public string Description => "Reloads stats from disk, discarding unsaved in-memory changes.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!AdminStatsCommand.AdminCheck(sender, out response)) return false;
        StatsSystemPlugin.Stats.Reload();
        response = "Stats reloaded from disk.";
        return true;
    }
}

internal sealed class ResetPlayerSubcommand : ICommand, IUsageProvider
{
    public string Command => "resetplayer";
    public string[] Aliases => ["reset", "rp"];
    public string Description => "Deletes all stats for a player. Usage: ss resetplayer <userId>";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!AdminStatsCommand.AdminCheck(sender, out response)) return false;
        if (arguments.Count == 0)
        {
            response = "Usage: ss resetplayer <userId>";
            return false;
        }

        var userId = arguments.At(0);
        if (!StatsSystemPlugin.Stats.DeletePlayerStats(userId))
        {
            response = $"No stats found for '{userId}'.";
            return false;
        }

        response = $"All stats deleted for '{userId}'.";
        return true;
    }

    public string[] Usage => ["<userId>"];
}

internal sealed class MigrateSubcommand : ICommand
{
    public string Command => "migrate";
    public string[] Aliases => ["m"];
    public string Description => "Repairs counter inconsistencies left by the v1 addstat/removestat bug.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!AdminStatsCommand.AdminCheck(sender, out response)) return false;
        var snapshot = StatsSystemPlugin.Stats.GetAllStatsSnapshot();
        response = V1Migrator.Repair(snapshot);
        return true;
    }
}

internal sealed class InfoSubcommand : ICommand
{
    public string Command => "info";
    public string[] Aliases => ["i"];
    public string Description => "Shows StatsSystem status information.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!AdminStatsCommand.AdminCheck(sender, out response)) return false;

        var snapshot = StatsSystemPlugin.Stats.GetAllStatsSnapshot();
        var allKeys = snapshot.Values
            .Where(s => s != null)
            .SelectMany(s => s.GetAllKeys())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var keyLine = allKeys.Count > 0 ? $"  Keys: {string.Join(", ", allKeys)}" : "  Keys: (none yet)";
        response = string.Join("\n",
            $"=== StatsSystem v{StatsSystemPlugin.Singleton.Version} ===",
            $"  Tracked players : {snapshot.Count}",
            $"  Known stat keys  : {allKeys.Count}",
            keyLine,
            $"  Auto-save every : {StatsSystemPlugin.Singleton.Config.AutoSaveIntervalSeconds}s"
        );
        return true;
    }
}