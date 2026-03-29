using System;
using CommandSystem;

namespace StatsSystem.ApiFeatures;

[CommandHandler(typeof(GameConsoleCommandHandler))]
public sealed class BearmanLogsStat : ICommand
{
    public string Command => "bearmanlogsStat";
    public string[] Aliases { get; } = ["bmlogsStat"];
    public string Description => "Uploads collected plugin logs to the log server and returns a log ID for support.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        var (result, success) = LogManager.GetLogHistory();
        response = result;
        return success;
    }
}