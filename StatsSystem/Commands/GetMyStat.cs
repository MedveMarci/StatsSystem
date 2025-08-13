using System;
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
            response = "Konzolból nem használhatod a parancsot!";
            return false;
        }

        var player = Player.Get(sender);

        if (player == null)
        {
            response = "Nem vagy játékos!";
            return false;
        }

        var stats = player.GetOrCreatePlayerStats();
        if (stats == null)
        {
            response = "Nincsenek statisztikáid!";
            return false;
        }

        if (!Events.EventHandler.PlayerJoinTimes.TryGetValue(player.UserId, out var joinTime))
        {
            response = "Nem található a belépési időd!";
            return false;
        }

        var playTimeSpan = DateTime.Now - joinTime;
        player.AddDuration("TotalPlayTime", playTimeSpan);
        Events.EventHandler.PlayerJoinTimes[player.UserId] = DateTime.Now;
        
        var deaths = (int)player.GetCounter("Deaths");
        var kills = (int)player.GetCounter("Kills");

        string FormatPlayTime(TimeSpan playTime)
        {
            var minutes = playTime.Minutes;
            var seconds = playTime.Seconds;
            var hours = playTime.Hours;
            var days = playTime.Days;

            if (days > 0)
                return $"{days} nap {hours} óra {minutes} perc {seconds} másodperc";
            if (hours > 0)
                return $"{hours} óra {minutes} perc {seconds} másodperc";
            if (minutes > 0)
                return $"{minutes} perc {seconds} másodperc";
            return $"{seconds} másodperc";
        }

        var totalPlayTime = player.GetDuration("TotalPlayTime");
        var kd = deaths == 0 ? kills : (float)kills / deaths;
        var kdFormatted = kd.ToString("F2");
        
        response = $"Statisztikáid:\n" +
                   $"- Játékidő: {FormatPlayTime(totalPlayTime)}\n" +
                   $"- Ölések: {kills}\n" +
                   $"- Halálok: {deaths}" +
                   (deaths > 0 ? $"\n- K/D arány: {kdFormatted}" : "");
        return true;
    }
}