using System;
using System.Collections.Concurrent;
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
        var issuer = Player.Get(sender);

        if (issuer == null)
        {
            response = "You must be a player to use this command.";
            return false;
        }

        if (!issuer.HasPermissions("stat.manage"))
        {
            response = "You do not have permission to use this command.";
            return false;
        }

        if (arguments.Count < 3)
        {
            response = $"Usage: {Command} {string.Join(" ", Usage)}";
            return false;
        }

        List<ReferenceHub> hubs = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out string[] remaining);
        bool hasOnline = hubs is { Count: > 0 };

        if (hasOnline && (remaining == null || remaining.Length < 2))
        {
            response = $"Usage: {Command} {string.Join(" ", Usage)}";
            return false;
        }

        string statKey  = hasOnline ? remaining![0] : arguments.At(1);
        string valueRaw = hasOnline ? remaining![1] : arguments.At(2);

        if (!long.TryParse(valueRaw, out long value))
        {
            response = "Value must be an integer.";
            return false;
        }

        if (hasOnline)
        {
            StringBuilder sb = StringBuilderPool.Shared.Rent();
            int affected = 0;

            foreach (ReferenceHub hub in hubs)
            {
                Player p = Player.Get(hub);
                if (p == null) continue;

                if (!StatsSystemPlugin.StatsSystem.TryGetOrCreatePlayerStats(p, out PlayerStats stats))
                    continue;

                if (!StatHelper.TrySet(stats, statKey, value, p.Nickname, out string line))
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

        return StatHelper.TrySet(offlineStats, statKey, value, query, out response);
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
        var issuer = Player.Get(sender);

        if (issuer == null)
        {
            response = "You must be a player to use this command.";
            return false;
        }

        if (!issuer.HasPermissions("stat.manage"))
        {
            response = "You do not have permission to use this command.";
            return false;
        }

        if (arguments.Count < 3)
        {
            response = $"Usage: {Command} {string.Join(" ", Usage)}";
            return false;
        }

        List<ReferenceHub> hubs = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out string[] remaining);
        bool hasOnline = hubs is { Count: > 0 };

        if (hasOnline && (remaining == null || remaining.Length < 2))
        {
            response = $"Usage: {Command} {string.Join(" ", Usage)}";
            return false;
        }

        string statKey   = hasOnline ? remaining![0] : arguments.At(1);
        string amountRaw = hasOnline ? remaining![1] : arguments.At(2);

        if (!long.TryParse(amountRaw, out long amount) || amount < 0)
        {
            response = "Amount must be a non-negative integer.";
            return false;
        }

        long   delta  = GetDelta(amount);
        string action = delta >= 0 ? "increased" : "decreased";

        if (hasOnline)
        {
            StringBuilder sb = StringBuilderPool.Shared.Rent();
            int affected = 0;

            foreach (ReferenceHub hub in hubs)
            {
                Player p = Player.Get(hub);
                if (p == null) continue;

                if (!StatsSystemPlugin.StatsSystem.TryGetOrCreatePlayerStats(p, out PlayerStats stats))
                    continue;

                if (!StatHelper.TryModify(stats, statKey, delta, action, p.Nickname, out string line))
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
            response = $"Player '{query}' was not found online and no saved stats exist for that identifier.";
            return false;
        }

        return StatHelper.TryModify(offlineStats, statKey, delta, action, query, out response);
    }

    protected abstract long GetDelta(long amount);
}

internal static class StatHelper
{
    private static string Today => DateTime.UtcNow.ToString("yyyy-MM-dd");
    
    internal static string ResolveOrCreateStatKey(PlayerStats stats, string statKey, out bool created)
    {
        string found = stats.DailyCounters.Keys
            .FirstOrDefault(k => string.Equals(k, statKey, StringComparison.OrdinalIgnoreCase));

        if (found != null)
        {
            created = false;
            return found;
        }

        stats.DailyCounters.GetOrAdd(statKey, _ => new ConcurrentDictionary<string, long>());
        created = true;
        return statKey;
    }

    internal static bool TryModify(PlayerStats stats, string statKey, long delta, string action,
        string targetName, out string response)
    {
        string resolvedKey = ResolveOrCreateStatKey(stats, statKey, out bool created);
        string today       = Today;

        var  perDay   = stats.DailyCounters.GetOrAdd(resolvedKey, _ => new ConcurrentDictionary<string, long>());
        long oldValue = perDay.GetOrAdd(today, 0);
        long newValue = perDay.AddOrUpdate(today, delta, (_, v) => v + delta);

        response = created
            ? $"{targetName}: created new stat '{resolvedKey}' with value {newValue}."
            : $"{targetName}: '{resolvedKey}' {action} from {oldValue} to {newValue}.";
        return true;
    }

    internal static bool TrySet(PlayerStats stats, string statKey, long value,
        string targetName, out string response)
    {
        string resolvedKey = ResolveOrCreateStatKey(stats, statKey, out bool created);
        string today       = Today;

        var  perDay   = stats.DailyCounters.GetOrAdd(resolvedKey, _ => new ConcurrentDictionary<string, long>());
        long oldValue = perDay.TryGetValue(today, out long ov) ? ov : 0;
        perDay[today] = value;

        response = created
            ? $"{targetName}: created new stat '{resolvedKey}' and set it to {value}."
            : $"{targetName}: '{resolvedKey}' set from {oldValue} to {value}.";
        return true;
    }
}
