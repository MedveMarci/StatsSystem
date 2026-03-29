using System.Collections.Generic;
using System.Linq;
using System.Text;
using StatsSystem.API;
using StatsSystem.ApiFeatures;

namespace StatsSystem.Migration;

internal static class V1Migrator
{
    internal static string Repair(IReadOnlyDictionary<string, PlayerStats> snapshot)
    {
        var sb = new StringBuilder();
        var players = 0;
        var fixes = 0;

        foreach (var kvp in snapshot)
        {
            var userId = kvp.Key;
            var stats = kvp.Value;
            if (stats == null) continue;

            var playerFixes = new List<string>();

            foreach (var kv in stats.DailyCounters)
            {
                var key = kv.Key;
                var perDay = kv.Value;
                if (perDay == null || perDay.Count == 0) continue;

                var dailyTotal = perDay.Values.Sum();
                var current = stats.GetCounter(key);

                if (dailyTotal > current)
                {
                    stats.Counters[key] = dailyTotal;
                    playerFixes.Add($"  {key}: {current} → {dailyTotal}");
                    fixes++;
                }
            }

            if (playerFixes.Count > 0)
            {
                sb.AppendLine($"Player {userId}:");
                foreach (var line in playerFixes) sb.AppendLine(line);
                players++;
            }
        }

        if (fixes == 0)
            return "Migration check complete. No inconsistencies found — data is already up to date.";

        sb.Insert(0, $"Migration repaired {fixes} counter(s) across {players} player(s):\n");
        LogManager.Info($"[V1Migrator] Repaired {fixes} counters for {players} players.");
        return sb.ToString().TrimEnd();
    }
}