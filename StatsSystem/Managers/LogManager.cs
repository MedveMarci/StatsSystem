using System;
using LabApi.Features.Console;

namespace StatsSystem.Managers;

internal abstract class LogManager
{
    private static bool DebugEnabled => StatsSystemPlugin.Singleton.Config.Debug;

    public static void Debug(string message)
    {
        if (!DebugEnabled)
            return;

        Logger.Raw($"[DEBUG] [{StatsSystemPlugin.Singleton.Name}] {message}", ConsoleColor.Green);
    }

    public static void Info(string message, ConsoleColor color = ConsoleColor.Cyan)
    {
        Logger.Raw($"[INFO] [{StatsSystemPlugin.Singleton.Name}] {message}", color);
    }

    public static void Warn(string message)
    {
        Logger.Warn(message);
    }

    public static void Error(string message)
    {
        Logger.Raw($"[ERROR] [{StatsSystemPlugin.Singleton.Name}] Details:\nVersion: {StatsSystemPlugin.Singleton.Version}\n{message}", ConsoleColor.Red);
    }
}