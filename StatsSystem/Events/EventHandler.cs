using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
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

    public override void OnServerRoundStarted()
    {
        if (StatsSystemPlugin.Singleton.Config.PlaytimeTracking)
        {
            PlayerJoinTimes.Clear();
            foreach (var player in Player.ReadyList)
            {
                if (player.DoNotTrack) continue;
                PlayerJoinTimes[player.UserId] = DateTime.Now;
                LogManager.Debug($"Player {player.UserId} joined at {PlayerJoinTimes[player.UserId]}");
            }
        }
        base.OnServerRoundStarted();
    }

    public override void OnServerRoundEnded(RoundEndedEventArgs ev)
    {
        if (StatsSystemPlugin.Singleton.Config.PlaytimeTracking)
        {
            foreach (var kvp in PlayerJoinTimes)
            {
                
                var player = Player.Get(kvp.Key);
                if (player == null) continue;
                if (player.DoNotTrack) continue;
                var playTime = DateTime.Now - kvp.Value;
                player.AddDuration("TotalPlayTime", playTime);
            }

            PlayerJoinTimes.Clear();
        }
        base.OnServerRoundEnded(ev);
    }

    public override void OnServerRoundRestarted()
    {
        if (StatsSystemPlugin.Singleton.Config.PlaytimeTracking)
        {
            PlayerJoinTimes.Clear();
            foreach (var player in Player.ReadyList)
            {
                if (player.DoNotTrack) continue;
                PlayerJoinTimes[player.UserId] = DateTime.Now;
                LogManager.Debug($"Player {player.UserId} joined at {PlayerJoinTimes[player.UserId]}");
            }
        }
        base.OnServerRoundRestarted();
    }

    public override void OnPlayerJoined(PlayerJoinedEventArgs ev)
    {
        if (!Round.IsRoundStarted) return;
        if (ev.Player.DoNotTrack) return;
        if (!StatsSystemPlugin.Singleton.Config.PlaytimeTracking) return;
        PlayerJoinTimes[ev.Player.UserId] = DateTime.Now;
        LogManager.Debug($"Player {ev.Player.UserId} joined at {PlayerJoinTimes[ev.Player.UserId]}");
        base.OnPlayerJoined(ev);
    }

    public override void OnPlayerLeft(PlayerLeftEventArgs ev)
    {
        if (!Round.IsRoundStarted) return;
        if (ev.Player.DoNotTrack) return;
        if (!StatsSystemPlugin.Singleton.Config.PlaytimeTracking) return;
        if (ev.Player == null || string.IsNullOrEmpty(ev.Player.UserId)) return;
        if (!PlayerJoinTimes.TryRemove(ev.Player.UserId, out var joinTime)) return;
        var playTime = DateTime.Now - joinTime;
        ev.Player.AddDuration("TotalPlayTime", playTime);
        LogManager.Debug($"Player {ev.Player.UserId} left after {playTime.TotalSeconds} seconds.");
        base.OnPlayerLeft(ev);
    }

    public override void OnPlayerDeath(PlayerDeathEventArgs ev)
    {
        if (!Round.IsRoundStarted) return;
        if (ev.Player.DoNotTrack) return;
        LogManager.Debug($"Player {ev.Player.UserId} died. Attacker: {ev.Attacker?.UserId ?? "None"}");
        if (StatsSystemPlugin.Singleton.Config.KillsTracking)
            ev.Attacker?.IncrementStat("Kills");
        if (StatsSystemPlugin.Singleton.Config.DeathsTracking)
            ev.Player.IncrementStat("Deaths");
        if (ev.OldRole is RoleTypeId.ClassD && StatsSystemPlugin.Singleton.Config.ClassDKillsTracking) 
            ev.Attacker?.IncrementStat("ClassDKills");
        if (ev.Attacker?.Role is RoleTypeId.ClassD && StatsSystemPlugin.Singleton.Config.KillsAsClassDTracking)
            ev.Attacker?.IncrementStat("KillsAsClassD");
        if (ev.OldRole.IsScp() && StatsSystemPlugin.Singleton.Config.ScpKillsTracking)
            ev.Attacker?.IncrementStat("ScpKills");
        if (ev.DamageHandler is MicroHidDamageHandler && StatsSystemPlugin.Singleton.Config.MicroHidKillsTracking)
            ev.Attacker?.IncrementStat("MicroHidKills");
        base.OnPlayerDeath(ev);
    }

    public override void OnServerWaitingForPlayers()
    {
        try
        {
            var currentVersion = StatsSystemPlugin.Singleton.Version; // snapshot
            _ = Task.Run(() => StatsSystemPlugin.CheckForUpdatesAsync(currentVersion));
        }
        catch (Exception ex)
        {
            LogManager.Error($"Version check could not be started.\n{ex}");
        }
        base.OnServerWaitingForPlayers();
    }

    internal static void OnQuit()
    {
        if (StatsSystemPlugin.Singleton.Config.PlaytimeTracking)
        {
            foreach (var kvp in PlayerJoinTimes)
            {
                var player = Player.Get(kvp.Key);
                if (player == null) continue;
                if (player.DoNotTrack) continue;
                var playTime = DateTime.Now - kvp.Value;
                player.AddDuration("TotalPlayTime", playTime);
            }

            PlayerJoinTimes.Clear();
        }
        StatsSystemPlugin.StatsSystem.SaveStats();
        LogManager.Debug("Player stats saved on quit.");
        Shutdown.OnQuit -= OnQuit;
    }
}