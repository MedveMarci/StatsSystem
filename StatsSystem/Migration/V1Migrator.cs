using System.Collections.Concurrent;
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
        StringBuilder sb = new();
        int players = 0;
        int fixes = 0;

        foreach (KeyValuePair<string, PlayerStats> kvp in snapshot)
        {
            string userId = kvp.Key;
            PlayerStats stats = kvp.Value;
            if (stats == null) continue;

            List<string> playerFixes = new();

            foreach (KeyValuePair<string, ConcurrentDictionary<string, long>> kv in stats.DailyCounters)
            {
                string key = kv.Key;
                ConcurrentDictionary<string, long> perDay = kv.Value;
                if (perDay == null || perDay.Count == 0) continue;

                long dailyTotal = perDay.Values.Sum();
                long current = stats.GetCounter(key);

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
                foreach (string line in playerFixes) sb.AppendLine(line);
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