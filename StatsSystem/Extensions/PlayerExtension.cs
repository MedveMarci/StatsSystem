using System;
using LabApi.Features.Wrappers;
using StatsSystem.API;

namespace StatsSystem.Extensions;

public static class PlayerExtension
{
    public static bool TryGetPlayerStats(this Player player, out PlayerStats stats)
    {
        return StatsSystemPlugin.StatsSystem.TryGetPlayerStats(player, out stats);
    }
    
    public static PlayerStats GetOrCreatePlayerStats(this Player player)
    {
        if (Events.EventHandler.PlayerJoinTimes.TryGetValue(player.UserId, out var joinTime))
        {
            var playTime = DateTime.Now - joinTime;
            player.UpdatePlayTime(playTime);
            Events.EventHandler.PlayerJoinTimes[player.UserId] = DateTime.Now;
        }
        return StatsSystemPlugin.StatsSystem.GetOrCreatePlayerStats(player);
    }
    
    public static void UpdatePlayTime(this Player player, TimeSpan time)
    {
        StatsSystemPlugin.StatsSystem.UpdatePlayTime(player, time);
    }
    
    public static void AddKill(this Player player)
    {
        StatsSystemPlugin.StatsSystem.AddKill(player);
    }
    
    public static void AddDeath(this Player player)
    {
        StatsSystemPlugin.StatsSystem.AddDeath(player);
    }
    
    public static TimeSpan GetPlayTime(this Player player)
    {
        return player.TryGetPlayerStats(out var stats) ? stats.TotalPlayTime : TimeSpan.Zero;
    }
    
    public static int GetKills(this Player player)
    {
        return player.TryGetPlayerStats(out var stats) ? stats.Kills : 0;
    }
    
    public static int GetDeaths(this Player player)
    {
        return player.TryGetPlayerStats(out var stats) ? stats.Deaths : 0;
    }
    
    public static float GetKdRatio(this Player player)
    {
        if (!player.TryGetPlayerStats(out var stats))
            return 0;
            
        return stats.Deaths == 0 ? stats.Kills : (float)stats.Kills / stats.Deaths;
    }
    
    public static string GetFormattedPlayTime(this Player player)
    {
        var playTime = player.GetPlayTime();
        return $"{playTime.Days}d {playTime.Hours}h {playTime.Minutes}m {playTime.Seconds}s";
    }
    
    public static string GetFormattedKdRatio(this Player player)
    {
        return player.GetKdRatio().ToString("F2");
    }
}