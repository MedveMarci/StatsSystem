using System;
using CommandSystem;

namespace StatsSystem.ApiFeatures;

[CommandHandler(typeof(GameConsoleCommandHandler))]
public class BearmanLogsStat : ICommand
{
    public string Command => "bearmanlogsStat";

    public string[] Aliases { get; } = ["bmlogsStat"];

    public string Description => "Sends collected plugin logs to the log server and returns the log id.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        var getLogHistory = LogManager.GetLogHistory();
        response = getLogHistory.logResult;
        return getLogHistory.success;
    }
}