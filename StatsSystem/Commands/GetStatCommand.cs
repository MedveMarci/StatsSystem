using System;
using System.Collections.Generic;
using System.Linq;
using CommandSystem;
using LabApi.Features.Wrappers;
using StatsSystem.API;
using StatsSystem.Extensions;
using EventHandler = StatsSystem.Events.EventHandler;

namespace StatsSystem.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class GetStatCommand : ICommand
{
    public string Command => "getstat";
    public string[] Aliases { get; } = ["gs"];
    public string Description => "Shows your stats, or another player's. Usage: .getstat [player|userId]";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        var requester = Player.Get(sender);

        Player targetPlayer = null;
        PlayerStats stats = null;
        string displayName = null;
        string lookupId = null;

        if (arguments.Count > 0)
        {
            var arg = arguments.At(0);
            targetPlayer = Player.Get(arg) ?? Player.GetByDisplayName(arg) ?? Player.GetByNickname(arg);

            if (targetPlayer != null)
            {
                if (!targetPlayer.TryGetOrCreatePlayerStats(out stats))
                {
                    response = $"Stats for '{targetPlayer.Nickname}' are not available (DoNotTrack).";
                    return false;
                }

                lookupId = targetPlayer.UserId;
                displayName = targetPlayer.Nickname;
            }
            else if (StatsSystemPlugin.Stats.TryGetStats(arg, out stats))
            {
                lookupId = arg;
                displayName = arg;
            }
            else
            {
                response = $"Player '{arg}' not found online and no saved stats exist for that identifier.";
                return false;
            }
        }
        else
        {
            if (requester == null)
            {
                response = "Usage: getstat <player|userId>";
                return false;
            }

            if (!requester.TryGetOrCreatePlayerStats(out stats))
            {
                response = "Your stats are not tracked (DoNotTrack is enabled for your account).";
                return false;
            }

            targetPlayer = requester;
            lookupId = requester.UserId;
            displayName = requester.Nickname;
        }

        if (StatsSystemPlugin.Singleton.Config.PlaytimeTracking && lookupId != null)
            EventHandler.FlushAndResetPlayer(lookupId);

        var lines = new List<string> { $"=== Stats: {displayName} ===" };

        if (stats.Counters.Count == 0 && stats.Durations.Count == 0)
        {
            lines.Add("No stats recorded yet.");
        }
        else
        {
            foreach (var kv in stats.Counters.OrderBy(k => k.Key))
                lines.Add($"  {kv.Key}: {kv.Value}");
            foreach (var kv in stats.Durations.OrderBy(k => k.Key))
                lines.Add($"  {kv.Key}: {FormatTime(kv.Value)}");
        }

        var lastDaysCfg = StatsSystemPlugin.Singleton?.Config?.LastDays;
        if (lastDaysCfg is { Count: > 0 } && (stats.Counters.Count > 0 || stats.Durations.Count > 0))
        {
            var periods = lastDaysCfg.Distinct().Where(d => d > 0).OrderBy(d => d).ToList();
            foreach (var days in periods)
            {
                lines.Add($"\n--- Last {days} days ---");
                foreach (var kv in stats.Counters.OrderBy(k => k.Key))
                {
                    var val = StatsSystemPlugin.Stats.GetLastDaysCounter(lookupId, kv.Key, days);
                    lines.Add($"  {kv.Key}: {val}");
                }

                foreach (var kv in stats.Durations.OrderBy(k => k.Key))
                {
                    var val = StatsSystemPlugin.Stats.GetLastDaysDuration(lookupId, kv.Key, days);
                    if (val > TimeSpan.Zero) lines.Add($"  {kv.Key}: {FormatTime(val)}");
                }
            }
        }

        response = string.Join("\n", lines);
        return true;
    }

    internal static string FormatTime(TimeSpan t)
    {
        if (t.Days > 0) return $"{t.Days}d {t.Hours}h {t.Minutes}m {t.Seconds}s";
        if (t.Hours > 0) return $"{t.Hours}h {t.Minutes}m {t.Seconds}s";
        if (t.Minutes > 0) return $"{t.Minutes}m {t.Seconds}s";
        return $"{t.Seconds}s";
    }
}
