using System;
using System.Collections.Generic;
using System.Text;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;
using NorthwoodLib.Pools;
using StatsSystem.API;
using Utils;

namespace StatsSystem.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class AddStatCommand : ModifyStatBase
{
    public override string Command => "addstat";

    public override string[] Aliases => [];

    public override string Description => "Increases a player's stat. Usage: addstat <player> <statKey> <amount>";

    protected override long GetDelta(long amount)
    {
        return amount;
    }
}

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class RemoveStatCommand : ModifyStatBase
{
    public override string Command => "removestat";

    public override string[] Aliases => [];

    public override string Description => "Decreases a player's stat. Usage: removestat <player> <statKey> <amount>";

    protected override long GetDelta(long amount)
    {
        return -amount;
    }
}

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class SetStatCommand : ICommand, IUsageProvider
{
    public string Command => "setstat";

    public string[] Aliases => [];

    public string Description => "Sets a player's stat to an exact value. Usage: setstat <player> <statKey> <value>";

    public string[] Usage => ["%player%", "<statKey>", "<value>"];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!ModifyStatHelper.CheckPermission(sender, out response)) return false;
        if (arguments.Count < 3)
        {
            response = $"Usage: {Command} {string.Join(" ", Usage)}";
            return false;
        }

        List<ReferenceHub> hubs = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out string[] remaining);
        bool hasOnline = hubs is { Count: > 0 };

        string statKey = hasOnline ? remaining?[0] ?? string.Empty : arguments.At(1);
        string valueRaw = hasOnline ? remaining?[1] ?? string.Empty : arguments.At(2);

        if (string.IsNullOrEmpty(statKey) || string.IsNullOrEmpty(valueRaw))
        {
            response = $"Usage: {Command} {string.Join(" ", Usage)}";
            return false;
        }

        if (!long.TryParse(valueRaw, out long value))
        {
            response = "Value must be an integer.";
            return false;
        }

        if (hasOnline)
        {
            response = ModifyStatHelper.ApplyToOnline(hubs, p =>
            {
                if (!StatsSystemPlugin.Stats.TryGetOrCreateStats(p, out PlayerStats s)) return null;
                long old = s.GetCounter(statKey);
                s.SetCounter(statKey, value);
                return $"{p.Nickname}: '{statKey}' set {old} → {value}";
            });
            return response != "No players were affected.";
        }

        string query = arguments.At(0);
        if (!StatsSystemPlugin.Stats.TryGetOrCreateStats(query, out PlayerStats stats))
        {
            response = $"Could not find or create stats for '{query}'.";
            return false;
        }

        long oldVal = stats.GetCounter(statKey);
        stats.SetCounter(statKey, value);
        response = $"{query}: '{statKey}' set {oldVal} → {value}";
        return true;
    }
}

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class DeleteStatCommand : ICommand, IUsageProvider
{
    public string Command => "deletestat";

    public string[] Aliases => [];

    public string Description =>
        "Removes a stat key entirely from a player's data. Usage: deletestat <userId> <statKey>";

    public string[] Usage => ["<userId>", "<statKey>"];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!ModifyStatHelper.CheckPermission(sender, out response)) return false;
        if (arguments.Count < 2)
        {
            response = $"Usage: {Command} {string.Join(" ", Usage)}";
            return false;
        }

        string userId = arguments.At(0);
        string statKey = arguments.At(1);

        if (!StatsSystemPlugin.Stats.DeleteStatKey(userId, statKey))
        {
            response = $"Stat key '{statKey}' not found for '{userId}'.";
            return false;
        }

        response = $"Deleted stat '{statKey}' from '{userId}'.";
        return true;
    }
}

public abstract class ModifyStatBase : ICommand, IUsageProvider
{
    public abstract string Command { get; }

    public abstract string[] Aliases { get; }

    public abstract string Description { get; }

    public string[] Usage => ["%player%", "<statKey>", "<amount>"];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!ModifyStatHelper.CheckPermission(sender, out response)) return false;
        if (arguments.Count < 3)
        {
            response = $"Usage: {Command} {string.Join(" ", Usage)}";
            return false;
        }

        List<ReferenceHub> hubs = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out string[] remaining);
        bool hasOnline = hubs is { Count: > 0 };

        string statKey = hasOnline ? remaining?[0] ?? string.Empty : arguments.At(1);
        string amountRaw = hasOnline ? remaining?[1] ?? string.Empty : arguments.At(2);

        if (string.IsNullOrEmpty(statKey) || string.IsNullOrEmpty(amountRaw))
        {
            response = $"Usage: {Command} {string.Join(" ", Usage)}";
            return false;
        }

        if (!long.TryParse(amountRaw, out long amount) || amount < 0)
        {
            response = "Amount must be a non-negative integer.";
            return false;
        }

        long delta = GetDelta(amount);
        string action = delta >= 0 ? "increased" : "decreased";

        if (hasOnline)
        {
            response = ModifyStatHelper.ApplyToOnline(hubs, p =>
            {
                if (!StatsSystemPlugin.Stats.TryGetOrCreateStats(p, out PlayerStats s)) return null;
                long old = s.GetCounter(statKey);
                s.IncrementCounter(statKey, delta);
                return $"{p.Nickname}: '{statKey}' {action} {old} → {old + delta}";
            });
            return response != "No players were affected.";
        }

        string query = arguments.At(0);
        if (!StatsSystemPlugin.Stats.TryGetOrCreateStats(query, out PlayerStats stats))
        {
            response = $"Player '{query}' not found and no saved stats exist for that identifier.";
            return false;
        }

        long oldVal = stats.GetCounter(statKey);
        stats.IncrementCounter(statKey, delta);
        response = $"{query}: '{statKey}' {action} {oldVal} → {oldVal + delta}";
        return true;
    }

    protected abstract long GetDelta(long amount);
}

internal static class ModifyStatHelper
{
    internal static bool CheckPermission(ICommandSender sender, out string response)
    {
        Player issuer = Player.Get(sender);
        if (issuer == null)
        {
            response = "This command can only be used as a player.";
            return false;
        }

        if (!issuer.HasPermissions("stat.manage"))
        {
            response = "Missing permission: stat.manage";
            return false;
        }

        response = null;
        return true;
    }

    internal static string ApplyToOnline(List<ReferenceHub> hubs, Func<Player, string> action)
    {
        StringBuilder sb = StringBuilderPool.Shared.Rent();
        int affected = 0;
        foreach (ReferenceHub hub in hubs)
        {
            Player p = Player.Get(hub);
            if (p == null) continue;
            string line = action(p);
            if (line == null) continue;
            if (affected > 0) sb.Append('\n');
            sb.Append(line);
            affected++;
        }

        string result = StringBuilderPool.Shared.ToStringReturn(sb);
        return affected > 0 ? result : "No players were affected.";
    }
}