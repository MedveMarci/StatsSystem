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
        
        var minutes = stats.TotalPlayTime.Minutes;
        var seconds = stats.TotalPlayTime.Seconds;
        var hours = stats.TotalPlayTime.Hours;
        var days = stats.TotalPlayTime.Days;

        string playTime;
        if (days > 0)
        {
            playTime = $"{days} nap ({hours} óra)";
        }
        else if (hours > 0)
        {
            playTime = $"{hours} óra {minutes} perc " + (seconds > 0 ? $"{seconds} másodperc" : "");
        }
        else if (minutes > 0)
        {
            playTime = $"{minutes} perc" + (seconds > 0 ? $" {seconds} másodperc" : "");
        }
        else
        {
            playTime = $"{seconds} másodperc";
        }

        response = $"Statisztikáid:\n" +
                   $"- Játékidő: {playTime}\n" +
                   $"- Ölések: {stats.Kills}\n" +
                   $"- Halálok: {stats.Deaths}";
        return true;
    }
}