using System;
using Logger = LabApi.Features.Console.Logger;

namespace StatsSystem.Managers;

internal class LogManager
{
    private static bool DebugEnabled => StatsSystemPlugin.Instance.Config.Debug;

    public static void Debug(string message)
    {
        if (!DebugEnabled)
            return;

        Logger.Raw($"[DEBUG] [{StatsSystemPlugin.Instance.Name}] {message}", ConsoleColor.Cyan);
    }

    public static void Info(string message, ConsoleColor color = ConsoleColor.Cyan)
    {
        Logger.Raw($"[INFO] [{StatsSystemPlugin.Instance.Name}] {message}", color);
    }

    public static void Warn(string message)
    {
        Logger.Warn(message);
    }

    public static void Error(string message)
    {
        Logger.Error(message);
    }
}