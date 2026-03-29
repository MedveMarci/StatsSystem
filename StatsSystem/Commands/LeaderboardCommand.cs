using System;
using System.Collections.Generic;
using System.Linq;
using CommandSystem;
using LabApi.Features.Wrappers;
using StatsSystem.API;
using EventHandler = StatsSystem.Events.EventHandler;

namespace StatsSystem.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class LeaderboardCommand : ICommand
{
    public string Command => "getleaderboard";
    public string[] Aliases { get; } = ["gl", "leaderboard"];

    public string Description =>
        "Shows the leaderboard for a stat. Usage: .gl <statKey> [top] | .gl <statKey> last <days> [top]";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (arguments.Count == 0)
        {
            response = "Usage: gl <statKey> [top] | gl <statKey> last <days> [top]";
            return false;
        }

        var statKey = arguments.At(0);
        var top = 10;
        int? lastDays = null;
        var idx = 1;

        if (arguments.Count > idx && string.Equals(arguments.At(idx), "last", StringComparison.OrdinalIgnoreCase))
        {
            idx++;
            if (arguments.Count <= idx || !int.TryParse(arguments.At(idx), out var days) || days <= 0)
            {
                response = "Usage: gl <statKey> last <days> [top]  —  days must be a positive integer.";
                return false;
            }

            lastDays = days;
            idx++;
        }

        if (arguments.Count > idx && !int.TryParse(arguments.At(idx), out top))
        {
            response = "Top count must be a positive integer. Example: gl Kills 15";
            return false;
        }

        if (top <= 0) top = 10;

        if (StatsSystemPlugin.Singleton.Config.PlaytimeTracking)
            foreach (var kvp in EventHandler.SessionStartTimes.ToArray())
                EventHandler.FlushAndResetPlayer(kvp.Key);

        var repo = (StatsRepository)StatsSystemPlugin.Stats;
        var snapshot = StatsSystemPlugin.Stats.GetAllStatsSnapshot();

        if (snapshot.Count == 0)
        {
            response = "No stats have been tracked yet.";
            return true;
        }

        var isDuration = snapshot.Values.Any(s => s?.Durations?.ContainsKey(statKey) == true);
        var isCounter = snapshot.Values.Any(s => s?.Counters?.ContainsKey(statKey) == true);

        if (!isDuration && !isCounter)
        {
            var known = repo.GetKnownKeys().ToList();
            if (known.Count == 0)
            {
                response = $"Unknown stat '{statKey}'. No stats are tracked yet.";
                return false;
            }

            const int max = 80;
            var shown = known.Take(max).ToList();
            var suffix = known.Count > max ? $"\n…and {known.Count - max} more" : string.Empty;
            response =
                $"Unknown stat '{statKey}'.\nAvailable stats ({known.Count}):\n- {string.Join("\n- ", shown)}{suffix}";
            return false;
        }

        if (isDuration)
        {
            var rows = new List<(string UserId, TimeSpan Value)>();
            foreach (var kvp in snapshot)
            {
                var userId = kvp.Key;
                var s = kvp.Value;
                var value = lastDays.HasValue
                    ? StatsSystemPlugin.Stats.GetLastDaysDuration(userId, statKey, lastDays.Value)
                    : s.GetDuration(statKey);
                if (value > TimeSpan.Zero) rows.Add((userId, value));
            }

            rows = rows.OrderByDescending(r => r.Value).Take(top).ToList();

            if (rows.Count == 0)
            {
                response = lastDays.HasValue
                    ? $"No entries for '{statKey}' in the last {lastDays.Value} days."
                    : $"No entries for '{statKey}'.";
                return true;
            }

            var header = lastDays.HasValue
                ? $"=== Leaderboard: {statKey} (last {lastDays.Value} days) ==="
                : $"=== Leaderboard: {statKey} ===";

            var lines = new List<string> { header };
            for (var i = 0; i < rows.Count; i++)
            {
                var (userId, value) = rows[i];
                var name = Player.Get(userId)?.Nickname ?? userId;
                lines.Add($"  {i + 1}. {name}: {GetStatCommand.FormatTime(value)}");
            }

            response = string.Join("\n", lines);
        }
        else
        {
            var rows = new List<(string UserId, long Value)>();
            foreach (var kvp in snapshot)
            {
                var userId = kvp.Key;
                var s = kvp.Value;
                var value = lastDays.HasValue
                    ? StatsSystemPlugin.Stats.GetLastDaysCounter(userId, statKey, lastDays.Value)
                    : s.GetCounter(statKey);
                if (value > 0) rows.Add((userId, value));
            }

            rows = rows.OrderByDescending(r => r.Value).Take(top).ToList();

            if (rows.Count == 0)
            {
                response = lastDays.HasValue
                    ? $"No entries for '{statKey}' in the last {lastDays.Value} days."
                    : $"No entries for '{statKey}'.";
                return true;
            }

            var header = lastDays.HasValue
                ? $"=== Leaderboard: {statKey} (last {lastDays.Value} days) ==="
                : $"=== Leaderboard: {statKey} ===";

            var lines = new List<string> { header };
            for (var i = 0; i < rows.Count; i++)
            {
                var (userId, value) = rows[i];
                var name = Player.Get(userId)?.Nickname ?? userId;
                lines.Add($"  {i + 1}. {name}: {value}");
            }

            response = string.Join("\n", lines);
        }

        return true;
    }
}