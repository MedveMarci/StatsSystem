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
        player.ModifyPlayTime(playTimeSpan);
        Events.EventHandler.PlayerJoinTimes[player.UserId] = DateTime.Now;
        
        var deaths = player.GetDeaths();
        var kills = player.GetKills();
        
        response = $"Statisztikáid:\n" +
                   $"- Játékidő: {player.GetFormattedPlayTime()}\n" +
                   $"- Ölések: {kills}\n" +
                   $"- Halálok: {deaths}"
                   + (deaths > 0 ? $"\n- K/D arány: {player.GetFormattedKdRatio()}" : "");
        return true;
    }
}