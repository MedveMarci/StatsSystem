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

        string statKey = arguments.At(0);
        int top = 10;
        int? lastDays = null;
        int idx = 1;

        if (arguments.Count > idx && string.Equals(arguments.At(idx), "last", StringComparison.OrdinalIgnoreCase))
        {
            idx++;
            if (arguments.Count <= idx || !int.TryParse(arguments.At(idx), out int days) || days <= 0)
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
            foreach (KeyValuePair<string, DateTime> kvp in EventHandler.SessionStartTimes.ToArray())
                EventHandler.FlushAndResetPlayer(kvp.Key);

        StatsRepository repo = (StatsRepository)StatsSystemPlugin.Stats;
        IReadOnlyDictionary<string, PlayerStats> snapshot = StatsSystemPlugin.Stats.GetAllStatsSnapshot();

        if (snapshot.Count == 0)
        {
            response = "No stats have been tracked yet.";
            return true;
        }

        bool isDuration = snapshot.Values.Any(s => s?.Durations?.ContainsKey(statKey) == true);
        bool isCounter = snapshot.Values.Any(s => s?.Counters?.ContainsKey(statKey) == true);

        if (!isDuration && !isCounter)
        {
            List<string> known = repo.GetKnownKeys().ToList();
            if (known.Count == 0)
            {
                response = $"Unknown stat '{statKey}'. No stats are tracked yet.";
                return false;
            }

            const int max = 80;
            List<string> shown = known.Take(max).ToList();
            string suffix = known.Count > max ? $"\n…and {known.Count - max} more" : string.Empty;
            response = $"Unknown stat '{statKey}'.\nAvailable stats ({known.Count}):\n- {string.Join("\n- ", shown)}{suffix}";
            return false;
        }

        if (isDuration)
        {
            List<(string UserId, TimeSpan Value)> rows = new();
            foreach (KeyValuePair<string, PlayerStats> kvp in snapshot)
            {
                string userId = kvp.Key;
                PlayerStats s = kvp.Value;
                TimeSpan value = lastDays.HasValue ? StatsSystemPlugin.Stats.GetLastDaysDuration(userId, statKey, lastDays.Value) : s.GetDuration(statKey);
                if (value > TimeSpan.Zero) rows.Add((userId, value));
            }

            rows = rows.OrderByDescending(r => r.Value).Take(top).ToList();

            if (rows.Count == 0)
            {
                response = lastDays.HasValue ? $"No entries for '{statKey}' in the last {lastDays.Value} days." : $"No entries for '{statKey}'.";
                return true;
            }

            string header = lastDays.HasValue ? $"=== Leaderboard: {statKey} (last {lastDays.Value} days) ===" : $"=== Leaderboard: {statKey} ===";

            List<string> lines = new() { header };
            for (int i = 0; i < rows.Count; i++)
            {
                (string userId, TimeSpan value) = rows[i];
                string name = Player.Get(userId)?.Nickname ?? userId;
                lines.Add($"  {i + 1}. {name}: {GetStatCommand.FormatTime(value)}");
            }

            response = string.Join("\n", lines);
        }
        else
        {
            List<(string UserId, long Value)> rows = new();
            foreach (KeyValuePair<string, PlayerStats> kvp in snapshot)
            {
                string userId = kvp.Key;
                PlayerStats s = kvp.Value;
                long value = lastDays.HasValue ? StatsSystemPlugin.Stats.GetLastDaysCounter(userId, statKey, lastDays.Value) : s.GetCounter(statKey);
                if (value > 0) rows.Add((userId, value));
            }

            rows = rows.OrderByDescending(r => r.Value).Take(top).ToList();

            if (rows.Count == 0)
            {
                response = lastDays.HasValue ? $"No entries for '{statKey}' in the last {lastDays.Value} days." : $"No entries for '{statKey}'.";
                return true;
            }

            string header = lastDays.HasValue ? $"=== Leaderboard: {statKey} (last {lastDays.Value} days) ===" : $"=== Leaderboard: {statKey} ===";

            List<string> lines = new() { header };
            for (int i = 0; i < rows.Count; i++)
            {
                (string userId, long value) = rows[i];
                string name = Player.Get(userId)?.Nickname ?? userId;
                lines.Add($"  {i + 1}. {name}: {value}");
            }

            response = string.Join("\n", lines);
        }

        return true;
    }
}