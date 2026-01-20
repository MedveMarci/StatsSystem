using System;
using System.Collections.Generic;
using System.Linq;
using CommandSystem;
using LabApi.Features.Wrappers;
using StatsSystem.Extensions;
using EventHandler = StatsSystem.Events.EventHandler;

namespace StatsSystem.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
public class GetLeaderboard : ICommand
{
    public string Command => "getleaderboard";

    public string[] Aliases { get; } = ["gl", "leaderboard"];

    public string Description => "Prints the leaderboard for a specific statistic or all.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (sender == null)
        {
            response = "You must be a player to use this command.";
            return false;
        }

        var requester = Player.Get(sender);
        if (requester == null)
        {
            response = "You must be a player to use this command.";
            return false;
        }

        if (arguments.Count <= 0)
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
                response = "Usage: gl <statKey> last <days> [top] (days must be a positive integer)";
                return false;
            }

            lastDays = days;
            idx++;
        }

        if (arguments.Count > idx)
            if (!int.TryParse(arguments.At(idx), out top) || top <= 0)
            {
                response = "Top must be a positive integer. Example: gl Kills 10";
                return false;
            }

        if (StatsSystemPlugin.Singleton.Config?.PlaytimeTracking == true)
            foreach (var kvp in EventHandler.PlayerJoinTimes.ToArray())
            {
                var userId = kvp.Key;
                if (!EventHandler.PlayerJoinTimes.TryGetValue(userId, out var joinTime)) continue;

                var player = Player.Get(userId);
                if (player == null || player.DoNotTrack) continue;

                var playTimeSpan = DateTime.Now - joinTime;
                player.AddDuration("TotalPlayTime", playTimeSpan);
                EventHandler.PlayerJoinTimes[userId] = DateTime.Now;
            }

        var snapshot = StatsSystemPlugin.StatsSystem.GetAllPlayerStatsSnapshot();
        if (snapshot.Count == 0)
        {
            response = "No stats tracked yet.";
            return true;
        }

        var isDuration = snapshot.Values.Any(s => s?.Durations != null && s.Durations.ContainsKey(statKey));
        var isCounter = snapshot.Values.Any(s => s?.Counters != null && s.Counters.ContainsKey(statKey));

        switch (isDuration)
        {
            case false when !isCounter:
            {
                var available = snapshot.Values
                    .Where(s => s != null)
                    .SelectMany(s => (s.Counters?.Keys ?? Array.Empty<string>())
                        .Concat(s.Durations?.Keys ?? Array.Empty<string>()))
                    .Where(k => !string.IsNullOrWhiteSpace(k))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                const int maxToShow = 80;
                var shown = available.Take(maxToShow).ToList();
                var suffix = available.Count > maxToShow
                    ? $"\n...and {available.Count - maxToShow} more"
                    : string.Empty;

                response = available.Count == 0
                    ? $"Unknown stat '{statKey}'. (No stats are tracked yet.)"
                    : $"Unknown stat '{statKey}'.\nAvailable stats ({available.Count}):\n- {string.Join("\n- ", shown)}{suffix}";
                return false;
            }
            case true:
            {
                var rows = new List<(string UserId, TimeSpan Value)>();
                foreach (var kvp in snapshot)
                {
                    var userId = kvp.Key;
                    var stats = kvp.Value;
                    if (stats == null) continue;

                    var value = lastDays.HasValue
                        ? StatsSystemPlugin.StatsSystem.GetPlayerLastDaysDuration(userId, statKey, lastDays.Value)
                        : stats.GetDuration(statKey);

                    if (value <= TimeSpan.Zero) continue;
                    rows.Add((userId, value));
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
                    ? $"Leaderboard: {statKey} (last {lastDays.Value} days)"
                    : $"Leaderboard: {statKey}";

                var lines = new List<string> { header };
                for (var i = 0; i < rows.Count; i++)
                {
                    var (userId, value) = rows[i];
                    var p = Player.Get(userId);
                    var name = p?.Nickname ?? userId;
                    lines.Add($"{i + 1}. {name}: {FormatTime(value)}");
                }

                response = string.Join("\n", lines);
                break;
            }
            default:
            {
                var rows = new List<(string UserId, long Value)>();
                foreach (var kvp in snapshot)
                {
                    var userId = kvp.Key;
                    var stats = kvp.Value;
                    if (stats == null) continue;

                    long value;
                    value = lastDays.HasValue
                        ? StatsSystemPlugin.StatsSystem.GetPlayerLastDaysCounter(userId, statKey, lastDays.Value)
                        : stats.GetCounter(statKey);

                    if (value <= 0) continue;
                    rows.Add((userId, value));
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
                    ? $"Leaderboard: {statKey} (last {lastDays.Value} days)"
                    : $"Leaderboard: {statKey}";

                var lines = new List<string> { header };
                for (var i = 0; i < rows.Count; i++)
                {
                    var (userId, value) = rows[i];
                    var p = Player.Get(userId);
                    var name = p?.Nickname ?? userId;
                    lines.Add($"{i + 1}. {name}: {value}");
                }

                response = string.Join("\n", lines);
                break;
            }
        }

        return true;

        static string FormatTime(TimeSpan playTime)
        {
            var minutes = playTime.Minutes;
            var seconds = playTime.Seconds;
            var hours = playTime.Hours;
            var days = playTime.Days;

            if (days > 0)
                return $"{days} days {hours} hours {minutes} minutes {seconds} seconds";
            if (hours > 0)
                return $"{hours} hours {minutes} minutes {seconds} seconds";
            if (minutes > 0)
                return $"{minutes} minutes {seconds} seconds";
            return $"{seconds} seconds";
        }
    }
}