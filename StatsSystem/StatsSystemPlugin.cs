using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using LabApi.Events.CustomHandlers;
using LabApi.Features;
using LabApi.Loader.Features.Paths;
using LabApi.Loader.Features.Plugins;
using StatsSystem.Managers;
using EventHandler = StatsSystem.Events.EventHandler;
using Version = System.Version;

namespace StatsSystem;

internal class StatsSystemPlugin : Plugin<Config>
{
    private const bool PreRelease = false;
    internal static StatsSystemPlugin Singleton;
    internal static API.StatsSystem StatsSystem;
    private static EventHandler _eventHandler;
    private static string _saveFilePath;
    private static string _saveFileDirectory;
    public override string Name => "StatsSystem";
    public override string Description => "StatSystem";
    public override string Author => "MedveMarci";
    public override Version Version { get; } = new(1, 1, 3);
    public override Version RequiredApiVersion => new(LabApiProperties.CompiledVersion);
    public override bool IsTransparent => true;
    public string githubRepo = "MedveMarci/StatsSystem";

    public override void Enable()
    {
        Singleton = this;
        _eventHandler = new EventHandler();

        Shutdown.OnQuit += EventHandler.OnQuit;
        CustomHandlersManager.RegisterEventsHandler(_eventHandler);

        _saveFileDirectory = Path.Combine(PathManager.Configs.FullName, Name);
        if (!Directory.Exists(_saveFileDirectory))
        {
            LogManager.Warn($"{Name} directory does not exist. Creating...");
            Directory.CreateDirectory(_saveFileDirectory);
        }

        _saveFilePath = Path.Combine(_saveFileDirectory, "player_stats.json");
        if (!File.Exists(_saveFilePath))
        {
            LogManager.Warn("Player stats file does not exist. Creating...");
            File.Create(_saveFilePath).Close();
        }

        LogManager.Info("Player stats will be saved to: " + _saveFilePath);
        StatsSystem = new API.StatsSystem(_saveFilePath);
        Task.Run(StartPeriodicSaving);
    }

    public override void Disable()
    {
        Shutdown.OnQuit -= EventHandler.OnQuit;
        CustomHandlersManager.UnregisterEventsHandler(_eventHandler);
        StatsSystem.SaveStats();
        LogManager.Info("Player stats saved to: " + _saveFilePath);
        StatsSystem = null;
        Singleton = null;
    }

    private static async Task StartPeriodicSaving()
    {
        while (ServerShutdown.ShutdownState == ServerShutdown.ServerShutdownState.NotInitiated)
        {
            LogManager.Debug("Player stats saved automatically.");
            await StatsSystem.SaveStatsAsync();
            await Task.Delay(TimeSpan.FromMinutes(1));
        }
    }

    internal static async Task CheckForUpdatesAsync(Version currentVersion)
    {
        try
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"{Singleton.Name}/{currentVersion}");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

            var repo = $"MedveMarci/{Singleton.Name}";
            var latestStableJson = await client.GetStringAsync($"https://api.github.com/repos/{repo}/releases/latest")
                .ConfigureAwait(false);
            var allReleasesJson = await client
                .GetStringAsync($"https://api.github.com/repos/{repo}/releases?per_page=20").ConfigureAwait(false);

            using var latestStableDoc = JsonDocument.Parse(latestStableJson);
            using var allReleasesDoc = JsonDocument.Parse(allReleasesJson);

            var latestStableRoot = latestStableDoc.RootElement;
            string stableTag = null;
            if (latestStableRoot.TryGetProperty("tag_name", out var tagProp))
                stableTag = tagProp.GetString();
            var stableVer = ParseVersion(stableTag);

            JsonElement? latestPre = null;
            Version preVer = null;
            string preTag = null;

            if (allReleasesDoc.RootElement.ValueKind == JsonValueKind.Array)
            {
                DateTime? bestPublishedAt = null;
                foreach (var rel in allReleasesDoc.RootElement.EnumerateArray())
                {
                    if (rel.ValueKind != JsonValueKind.Object) continue;

                    var draft = rel.TryGetProperty("draft", out var draftProp) &&
                                draftProp.ValueKind == JsonValueKind.True;
                    if (draft) continue;

                    var prerelease = rel.TryGetProperty("prerelease", out var preProp) &&
                                     preProp.ValueKind == JsonValueKind.True;
                    if (!prerelease) continue;

                    DateTime? publishedAt = null;
                    if (rel.TryGetProperty("published_at", out var pubProp))
                    {
                        var s = pubProp.GetString();
                        if (!string.IsNullOrWhiteSpace(s) && DateTime.TryParse(s, out var dt))
                            publishedAt = dt;
                    }

                    if (latestPre == null || (publishedAt.HasValue &&
                                              (!bestPublishedAt.HasValue || publishedAt.Value > bestPublishedAt.Value)))
                    {
                        latestPre = rel;
                        bestPublishedAt = publishedAt;
                    }
                }
            }

            if (latestPre.HasValue)
            {
                if (latestPre.Value.TryGetProperty("tag_name", out var preTagProp))
                    preTag = preTagProp.GetString();
                preVer = ParseVersion(preTag);
            }

            var outdatedStable = stableVer != null && stableVer > currentVersion;
            var prereleaseNewer = preVer != null && preVer > currentVersion && !outdatedStable;

            if (outdatedStable)
                LogManager.Info(
                    $"A new {Singleton.Name} version is available: {stableTag} (current {currentVersion}). Download: https://github.com/MedveMarci/{Singleton.Name}/releases/latest",
                    ConsoleColor.DarkRed);
            else if (prereleaseNewer)
                LogManager.Info(
                    $"A newer pre-release is available: {preTag} (current {currentVersion}). Download: https://github.com/MedveMarci/{Singleton.Name}/releases/tag/{preTag}",
                    ConsoleColor.DarkYellow);
            else
                LogManager.Info(
                    $"Thanks for using {Singleton.Name} v{currentVersion}. To get support and latest news, join to my Discord Server: https://discord.gg/KmpA8cfaSA",
                    ConsoleColor.Blue);
            if (PreRelease)
                LogManager.Info(
                    "This is a pre-release version. There might be bugs, if you find one, please report it on GitHub or Discord.",
                    ConsoleColor.DarkYellow);
        }
        catch (Exception e)
        {
            LogManager.Error($"Version check failed.\n{e}");
        }
    }

    private static Version ParseVersion(string tag)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tag)) return null;
            var t = tag.Trim();
            if (t.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                t = t.Substring(1);

            var cut = t.IndexOfAny(['-', '+']);
            if (cut >= 0)
                t = t.Substring(0, cut);

            return Version.TryParse(t, out var v) ? v : null;
        }
        catch
        {
            return null;
        }
    }
}