using System;
using System.Collections.Concurrent;
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
        if (LabApi.Features.Wrappers.Round.IsRoundStarted && !ev.Player.DoNotTrack &&
            StatsSystemPlugin.Singleton.Config.PlaytimeTracking)
        {
            SessionStartTimes[ev.Player.UserId] = DateTime.Now;
            LogManager.Debug($"Session started: {ev.Player.UserId}");
        }

        base.OnPlayerJoined(ev);
    }

    public override void OnPlayerLeft(PlayerLeftEventArgs ev)
    {
        if (!LabApi.Features.Wrappers.Round.IsRoundStarted || ev.Player?.UserId == null ||
            ev.Player.DoNotTrack || !StatsSystemPlugin.Singleton.Config.PlaytimeTracking)
        {
            base.OnPlayerLeft(ev);
            return;
        }

        if (SessionStartTimes.TryRemove(ev.Player.UserId, out var start))
        {
            var elapsed = DateTime.Now - start;
            ev.Player.AddDuration("TotalPlayTime", elapsed);
            LogManager.Debug($"Playtime flushed for {ev.Player.UserId}: {elapsed.TotalSeconds:F0}s");
        }

        base.OnPlayerLeft(ev);
    }

    public override void OnPlayerDeath(PlayerDeathEventArgs ev)
    {
        if (!LabApi.Features.Wrappers.Round.IsRoundStarted)
        {
            base.OnPlayerDeath(ev);
            return;
        }

        var cfg = StatsSystemPlugin.Singleton.Config;
        var attacker = ev.Attacker;
        var victim = ev.Player;

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
        try
        {
            ApiManager.CheckForUpdates();
        }
        catch (Exception ex)
        {
            LogManager.Error($"Version check failed: {ex.Message}");
        }

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
        foreach (var player in Player.ReadyList)
        {
            if (player.DoNotTrack) continue;
            SessionStartTimes[player.UserId] = DateTime.Now;
            LogManager.Debug($"Session started: {player.UserId}");
        }
    }

    private static void FlushAllPlaytimes()
    {
        var now = DateTime.Now;
        foreach (var kvp in SessionStartTimes)
        {
            var userId = kvp.Key;
            var start = kvp.Value;
            var player = Player.Get(userId);
            if (player == null || player.DoNotTrack) continue;
            var elapsed = now - start;
            player.AddDuration("TotalPlayTime", elapsed);
            LogManager.Debug($"Playtime flushed for {userId}: {elapsed.TotalSeconds:F0}s");
        }

        SessionStartTimes.Clear();
    }

    internal static void FlushAndResetPlayer(string userId)
    {
        if (!SessionStartTimes.TryGetValue(userId, out var start)) return;
        var player = Player.Get(userId);
        if (player == null || player.DoNotTrack) return;
        var elapsed = DateTime.Now - start;
        player.AddDuration("TotalPlayTime", elapsed);
        SessionStartTimes[userId] = DateTime.Now;
    }
}