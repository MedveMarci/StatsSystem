using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;
using NorthwoodLib.Pools;
using StatsSystem.API;
using Utils;

namespace StatsSystem.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class AddStat : ModifyStatCommandBase
{
    public override string Command => "addstat";
    public override string[] Aliases => [];
    public override string Description => "Increases a player's stat by the provided amount. Usage: addstat <player> <statKey> <amount>";
    protected override long GetDelta(long amount) => amount;
}

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class RemoveStat : ModifyStatCommandBase
{
    public override string Command => "removestat";
    public override string[] Aliases => [];
    public override string Description => "Decreases a player's stat by the provided amount. Usage: removestat <player> <statKey> <amount>";
    protected override long GetDelta(long amount) => -amount;
}

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class SetStat : ICommand, IUsageProvider
{
    public string Command => "setstat";
    public string[] Aliases => [];
    public string Description => "Sets a player's stat to the provided value. Usage: setstat <player> <statKey> <value>";
    public string[] Usage => ["%player%", "<statKey>", "<value>"];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        var player = Player.Get(sender);

        if (player == null)
        {
            response = "You must be a player to use this command.";
            return false;
        }

        if (!player.HasPermissions("stat.manage"))
        {
            response = "You do not have permission to use this command.";
            return false;
        }

        if (arguments.Count < 3)
        {
            response = $"Usage: {Command} <player> <statKey> <value>";
            return false;
        }

        List<ReferenceHub> hubs = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out string[] newargs);
        bool hasOnlinePlayers = hubs is { Count: > 0 };

        string statKey = hasOnlinePlayers ? newargs[0] : arguments.At(1);
        string valueRaw = hasOnlinePlayers ? newargs[1] : arguments.At(2);

        if (!long.TryParse(valueRaw, out long value))
        {
            response = "Value must be an integer.";
            return false;
        }

        if (hasOnlinePlayers)
        {
            int affected = 0;
            StringBuilder sb = StringBuilderPool.Shared.Rent();

            foreach (ReferenceHub hub in hubs)
            {
                Player player1 = Player.Get(hub);
                if (player1 == null) continue;

                if (!StatsSystemPlugin.StatsSystem.TryGetOrCreatePlayerStats(player1, out PlayerStats stats))
                    continue;

                if (!TrySetStat(stats, statKey, value, player1.Nickname, out string line))
                {
                    response = line;
                    StringBuilderPool.Shared.Return(sb);
                    return false;
                }

                if (affected > 0) sb.Append('\n');
                sb.Append(line);
                affected++;
            }

            string result = sb.ToString();
            StringBuilderPool.Shared.Return(sb);

            response = affected > 0 ? result : "No players were affected.";
            return affected > 0;
        }

        string query = arguments.At(0);
        if (!StatsSystemPlugin.StatsSystem.TryGetOrCreatePlayerStats(query, out PlayerStats offlineStats))
        {
            response = $"Could not resolve or create stats for '{query}'.";
            return false;
        }

        return TrySetStat(offlineStats, statKey, value, query, out response);
    }

    private static bool TrySetStat(PlayerStats stats, string statKey, long value, string targetName, out string response)
    {
        string resolvedStatKey = ModifyStatCommandBase.ResolveOrCreateStatKey(stats, statKey, out bool created);
        bool isCounter = stats.Counters.ContainsKey(resolvedStatKey);

        if (isCounter)
        {
            long oldValue = stats.Counters[resolvedStatKey];
            stats.Counters[resolvedStatKey] = value;
            response = created
                ? $"{targetName}: created new stat '{resolvedStatKey}' and set it to {value}."
                : $"{targetName}: '{resolvedStatKey}' set from {oldValue} to {value}.";
        }
        else
        {
            TimeSpan oldValue = stats.Durations[resolvedStatKey];
            stats.Durations[resolvedStatKey] = TimeSpan.FromSeconds(value);
            response = $"{targetName}: '{resolvedStatKey}' set from {oldValue} to {TimeSpan.FromSeconds(value)}.";
        }

        return true;
    }
}

public abstract class ModifyStatCommandBase : ICommand, IUsageProvider
{
    public abstract string Command { get; }
    public abstract string[] Aliases { get; }
    public abstract string Description { get; }
    public string[] Usage => ["%player%", "<statKey>", "<amount>"];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        var player = Player.Get(sender);

        if (player == null)
        {
            response = "You must be a player to use this command.";
            return false;
        }

        if (!player.HasPermissions("stat.manage"))
        {
            response = "You do not have permission to use this command.";
            return false;
        }

        if (arguments.Count < 3)
        {
            response = $"Usage: {Command} <player> <statKey> <amount>";
            return false;
        }

        List<ReferenceHub> hubs = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out string[] newargs);
        bool hasOnlinePlayers = hubs is { Count: > 0 };

        string statKey = hasOnlinePlayers ? newargs[0] : arguments.At(1);
        string amountRaw = hasOnlinePlayers ? newargs[1] : arguments.At(2);

        if (!long.TryParse(amountRaw, out long amount) || amount < 0)
        {
            response = "Amount must be a non-negative integer.";
            return false;
        }

        long delta = GetDelta(amount);
        string action = delta >= 0 ? "increased" : "decreased";

        if (hasOnlinePlayers)
        {
            int affected = 0;
            StringBuilder sb = StringBuilderPool.Shared.Rent();

            foreach (ReferenceHub hub in hubs)
            {
                Player player1 = Player.Get(hub);
                if (player1 == null) continue;

                if (!StatsSystemPlugin.StatsSystem.TryGetOrCreatePlayerStats(player1, out PlayerStats stats))
                    continue;

                if (!TryModifyStat(stats, statKey, delta, action, player1.Nickname, out string line))
                {
                    response = line;
                    StringBuilderPool.Shared.Return(sb);
                    return false;
                }

                if (affected > 0) sb.Append('\n');
                sb.Append(line);
                affected++;
            }

            string result = sb.ToString();
            StringBuilderPool.Shared.Return(sb);

            response = affected > 0 ? result : "No players were affected.";
            return affected > 0;
        }

        string query = arguments.At(0);
        if (!StatsSystemPlugin.StatsSystem.TryGetOrCreatePlayerStats(query, out PlayerStats offlineStats))
        {
            response = $"Player '{query}' was not found online, and no saved stats exist for that identifier.";
            return false;
        }

        return TryModifyStat(offlineStats, statKey, delta, action, query, out response);
    }

    internal static string ResolveOrCreateStatKey(PlayerStats stats, string statKey, out bool created)
    {
        string resolvedStatKey = stats.Counters.Keys
            .Concat(stats.Durations.Keys)
            .FirstOrDefault(key => string.Equals(key, statKey, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(resolvedStatKey))
        {
            created = false;
            return resolvedStatKey;
        }

        stats.Counters[statKey] = 0;
        created = true;
        return statKey;
    }

    private static bool TryModifyStat(PlayerStats stats, string statKey, long delta, string action, string targetName, out string response)
    {
        string resolvedStatKey = ResolveOrCreateStatKey(stats, statKey, out bool created);
        bool isCounter = stats.Counters.ContainsKey(resolvedStatKey);

        if (isCounter)
        {
            long oldValue = stats.Counters[resolvedStatKey];
            stats.Counters[resolvedStatKey] = oldValue + delta;
            response = created
                ? $"{targetName}: created new stat '{resolvedStatKey}' with value {stats.Counters[resolvedStatKey]}."
                : $"{targetName}: '{resolvedStatKey}' {action} from {oldValue} to {stats.Counters[resolvedStatKey]}.";
        }
        else
        {
            TimeSpan oldValue = stats.Durations[resolvedStatKey];
            stats.Durations[resolvedStatKey] = oldValue + TimeSpan.FromSeconds(delta);
            response = $"{targetName}: '{resolvedStatKey}' {action} from {oldValue} to {stats.Durations[resolvedStatKey]}.";
        }

        return true;
    }

    protected abstract long GetDelta(long amount);
}
