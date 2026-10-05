using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.CustomHandlers;
using LabApi.Features.Extensions;
using LabApi.Features.Wrappers;
using PlayerRoles;
using PlayerStatsSystem;
using StatsSystem.ApiFeatures;
using StatsSystem.Extensions;

namespace StatsSystem.Events;

internal sealed class EventHandler : CustomEventsHandler
{
    internal static readonly ConcurrentDictionary<string, DateTime> SessionStartTimes = new();

    public override void OnServerRoundStarted()
    {
        if (StatsSystemPlugin.Singleton.Config.PlaytimeTracking)
            RecordAllSessionStarts();
        base.OnServerRoundStarted();
    }

    public override void OnServerRoundEnded(RoundEndedEventArgs ev)
    {
        if (StatsSystemPlugin.Singleton.Config.PlaytimeTracking)
            FlushAllPlaytimes();
        base.OnServerRoundEnded(ev);
    }

    public override void OnServerRoundRestarted()
    {
        if (StatsSystemPlugin.Singleton.Config.PlaytimeTracking)
        {
            FlushAllPlaytimes();
            RecordAllSessionStarts();
        }

        base.OnServerRoundRestarted();
    }

    public override void OnPlayerJoined(PlayerJoinedEventArgs ev)
    {
        if (Round.IsRoundStarted && !ev.Player.DoNotTrack && StatsSystemPlugin.Singleton.Config.PlaytimeTracking)
        {
            SessionStartTimes[ev.Player.UserId] = DateTime.Now;
            LogManager.Debug($"Session started: {ev.Player.UserId}");
        }

        base.OnPlayerJoined(ev);
    }

    public override void OnPlayerLeft(PlayerLeftEventArgs ev)
    {
        if (!Round.IsRoundStarted || ev.Player?.UserId == null || ev.Player.DoNotTrack || !StatsSystemPlugin.Singleton.Config.PlaytimeTracking)
        {
            base.OnPlayerLeft(ev);
            return;
        }

        if (SessionStartTimes.TryRemove(ev.Player.UserId, out DateTime start))
        {
            TimeSpan elapsed = DateTime.Now - start;
            ev.Player.AddDuration("TotalPlayTime", elapsed);
            LogManager.Debug($"Playtime flushed for {ev.Player.UserId}: {elapsed.TotalSeconds:F0}s");
        }

        base.OnPlayerLeft(ev);
    }

    public override void OnPlayerDeath(PlayerDeathEventArgs ev)
    {
        if (!Round.IsRoundStarted)
        {
            base.OnPlayerDeath(ev);
            return;
        }

        Config cfg = StatsSystemPlugin.Singleton.Config;
        Player attacker = ev.Attacker;
        Player victim = ev.Player;

        if (cfg.KillsTracking && attacker is { DoNotTrack: false })
            attacker.IncrementStat("Kills");

        if (cfg.DeathsTracking && victim is { DoNotTrack: false })
            victim.IncrementStat("Deaths");

        if (attacker is { DoNotTrack: false })
        {
            if (cfg.ClassDKillsTracking && ev.OldRole == RoleTypeId.ClassD)
                attacker.IncrementStat("ClassDKills");

            if (cfg.KillsAsClassDTracking && attacker.Role == RoleTypeId.ClassD)
                attacker.IncrementStat("KillsAsClassD");

            if (cfg.ScpKillsTracking && ev.OldRole.IsScp())
                attacker.IncrementStat("ScpKills");

            if (cfg.MicroHidKillsTracking && ev.DamageHandler is MicroHidDamageHandler)
                attacker.IncrementStat("MicroHidKills");
        }

        base.OnPlayerDeath(ev);
    }

    public override void OnServerWaitingForPlayers()
    {
        VersionManager.CheckForUpdates();

        base.OnServerWaitingForPlayers();
    }

    internal static void OnQuit()
    {
        if (StatsSystemPlugin.Singleton?.Config?.PlaytimeTracking == true)
            FlushAllPlaytimes();

        StatsSystemPlugin.Stats?.Save();
        LogManager.Info("Stats saved on server shutdown.");
    }

    private static void RecordAllSessionStarts()
    {
        SessionStartTimes.Clear();
        foreach (Player player in Player.ReadyList)
        {
            if (player.DoNotTrack) continue;
            SessionStartTimes[player.UserId] = DateTime.Now;
            LogManager.Debug($"Session started: {player.UserId}");
        }
    }

    private static void FlushAllPlaytimes()
    {
        DateTime now = DateTime.Now;
        foreach (KeyValuePair<string, DateTime> kvp in SessionStartTimes)
        {
            string userId = kvp.Key;
            DateTime start = kvp.Value;
            Player player = Player.Get(userId);
            if (player == null || player.DoNotTrack) continue;
            TimeSpan elapsed = now - start;
            player.AddDuration("TotalPlayTime", elapsed);
            LogManager.Debug($"Playtime flushed for {userId}: {elapsed.TotalSeconds:F0}s");
        }

        SessionStartTimes.Clear();
    }

    internal static void FlushAndResetPlayer(string userId)
    {
        if (!SessionStartTimes.TryGetValue(userId, out DateTime start)) return;
        Player player = Player.Get(userId);
        if (player == null || player.DoNotTrack) return;
        TimeSpan elapsed = DateTime.Now - start;
        player.AddDuration("TotalPlayTime", elapsed);
        SessionStartTimes[userId] = DateTime.Now;
    }
}