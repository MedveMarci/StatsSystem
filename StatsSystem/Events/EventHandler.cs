using System;
using System.Collections.Concurrent;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.CustomHandlers;
using LabApi.Features.Extensions;
using LabApi.Features.Wrappers;
using PlayerRoles;
using PlayerStatsSystem;
using StatsSystem.Extensions;
using StatsSystem.Managers;

namespace StatsSystem.Events;

internal class EventHandler : CustomEventsHandler
{
    internal static readonly ConcurrentDictionary<string, DateTime> PlayerJoinTimes = new();
    
    public override void OnPlayerJoined(PlayerJoinedEventArgs ev)
    {
        PlayerJoinTimes[ev.Player.UserId] = DateTime.Now;
        LogManager.Debug($"Player {ev.Player.UserId} joined at {PlayerJoinTimes[ev.Player.UserId]}");
        base.OnPlayerJoined(ev);
    }

    public override void OnPlayerLeft(PlayerLeftEventArgs ev)
    {
        if (!PlayerJoinTimes.TryRemove(ev.Player.UserId, out var joinTime)) return;
        var playTime = DateTime.Now - joinTime;
        ev.Player.AddDuration("TotalPlayTime", playTime);
        LogManager.Debug($"Player {ev.Player.UserId} left after {playTime.TotalSeconds} seconds.");
        base.OnPlayerLeft(ev);
    }

    public override void OnPlayerDeath(PlayerDeathEventArgs ev)
    {
        LogManager.Debug($"Player {ev.Player.UserId} died. Attacker: {ev.Attacker?.UserId ?? "None"}");
        ev.Attacker?.IncrementStat("Kills");
        ev.Player.IncrementStat("Deaths");
        if (ev.OldRole is RoleTypeId.ClassD) 
            ev.Attacker?.IncrementStat("ClassDKills");
        if (ev.Attacker?.Role is RoleTypeId.ClassD)
            ev.Attacker?.IncrementStat("KillsAsClassD");
        if (ev.OldRole.IsScp())
            ev.Attacker?.IncrementStat("ScpKills");
        if (ev.DamageHandler is MicroHidDamageHandler)
            ev.Attacker?.IncrementStat("MicroHidKills");
        base.OnPlayerDeath(ev);
    }
    
    internal static void OnQuit()
    {
        foreach (var kvp in PlayerJoinTimes)
        {
            var player = Player.Get(kvp.Key);
            if (player == null) continue;
            var playTime = DateTime.Now - kvp.Value;
            player.AddDuration("TotalPlayTime", playTime);
        }
        
        PlayerJoinTimes.Clear();
        StatsSystemPlugin.StatsSystem.SaveStats();
        LogManager.Debug("Player stats saved on quit.");
        Shutdown.OnQuit -= OnQuit;
    }
}