using System;
using System.Collections.Generic;
using System.Linq;
using CommandSystem;
using LabApi.Features.Wrappers;
using StatsSystem.Extensions;

namespace StatsSystem.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
public class ScpLeave : ICommand
{
    public string Command => "getstat";

    public string[] Aliases { get; } = ["gs"];

    public string Description => "Prints your stats.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (sender == null)
        {
            response = "You must be a player to use this command.";
            return false;
        }

        var player = Player.Get(sender);

        if (player == null)
        {
            response = "Player not found.";
            return false;
        }

        var stats = player.GetOrCreatePlayerStats();
        if (stats == null)
        {
            response = "Your stats could not be found or created.";
            return false;
        }

        if (Events.EventHandler.PlayerJoinTimes.TryGetValue(player.UserId, out var joinTime))
        {
            var playTimeSpan = DateTime.Now - joinTime;
            player.AddDuration("TotalPlayTime", playTimeSpan);
            Events.EventHandler.PlayerJoinTimes[player.UserId] = DateTime.Now;
        }

        var statLines = new List<string> { "Statistics:" };
        statLines.AddRange(stats.Counters.Select(kvp => $"- {kvp.Key}: {kvp.Value}"));

        statLines.AddRange(stats.Durations.Select(kvp => $"- {kvp.Key}: {FormatTime(kvp.Value)}"));

        response = string.Join("\n", statLines);
        return true;

        string FormatTime(TimeSpan playTime)
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