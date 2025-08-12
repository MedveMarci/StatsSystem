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
        return StatsSystemPlugin.StatsSystem.GetOrCreatePlayerStats(player);
    }
    
    public static void ModifyStat(this Player player, StatType type, int amount = 1)
    {
        StatsSystemPlugin.StatsSystem.ModifyPlayerStat(player, type, amount);
    }

    public static void ModifyPlayTime(this Player player, TimeSpan time)
    {
        StatsSystemPlugin.StatsSystem.ModifyPlayerPlayTime(player, time);
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
        
        var minutes = playTime.Minutes;
        var seconds = playTime.Seconds;
        var hours = playTime.Hours;
        var days = playTime.Days;
        
        string playTimeString;
        if (days > 0)
        {
            playTimeString = $"{days} nap {hours} óra {minutes} perc {seconds} másodperc";
        }
        else if (hours > 0)
        {
            playTimeString = $"{hours} óra {minutes} perc 0 másodperc";
        }
        else if (minutes > 0)
        {
            playTimeString = $"{minutes} perc {seconds} másodperc";
        }
        else
        {
            playTimeString = $"{seconds} másodperc";
        }

        return playTimeString;
    }
    
    public static string GetFormattedKdRatio(this Player player)
    {
        return player.GetKdRatio().ToString("F2");
    }
}