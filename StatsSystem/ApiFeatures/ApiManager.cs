using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using LabApi.Features;

namespace StatsSystem.ApiFeatures;

internal static class ApiManager
{
    private const string ApiBase = "https://bearmanapi.hu";
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(8);

    internal static void CheckForUpdates()
    {
        Task.Run(async () =>
        {
            var name = StatsSystemPlugin.Singleton.Name;
            var current = StatsSystemPlugin.Singleton.Version;

            try
            {
                var resp = await WithTimeout(
                    HttpQuery.GetAsync($"{ApiBase}/api/v1/plugin/{Uri.EscapeDataString(name)}/latest"));

                var (code, _) = ParseResponse(resp);
                if (code != HttpStatusCode.OK)
                {
                    LogManager.Error($"Version check failed: {code}");
                    return;
                }

                var root = JsonDocument.Parse(resp).RootElement;
                if (!root.TryGetProperty("version", out var vProp) || vProp.ValueKind != JsonValueKind.String ||
                    !Version.TryParse(vProp.GetString() ?? "", out var latest))
                {
                    LogManager.Error("Version check: invalid response format.");
                    return;
                }

                var verResp = await WithTimeout(
                    HttpQuery.GetAsync(
                        $"{ApiBase}/api/v1/plugin/{Uri.EscapeDataString(name)}/version/{Uri.EscapeDataString(current.ToString())}"));

                var recallDoc = JsonDocument.Parse(verResp).RootElement;
                if (recallDoc.TryGetProperty("is_recalled", out var recalled) &&
                    recalled.ValueKind == JsonValueKind.True)
                {
                    var reason = recallDoc.TryGetProperty("recall_reason", out var r) &&
                                 r.ValueKind == JsonValueKind.String
                        ? r.GetString()
                        : "No reason provided.";
                    LogManager.Error(
                        $"This version of {name} has been recalled! Update to {latest} ASAP.\nReason: {reason}",
                        ConsoleColor.DarkRed);
                    return;
                }

                if (latest > current)
                    LogManager.Info(
                        $"New version of {name} available: {latest} (you have {current}). {GetDownloadUrl(root)}",
                        ConsoleColor.DarkRed);
                else
                    LogManager.Info($"Thank you for using {name} v{current}. Support: https://discord.gg/KmpA8cfaSA",
                        ConsoleColor.Blue);

                if (current > latest)
                    LogManager.Info(
                        $"You are running a newer version of {StatsSystemPlugin.Singleton.Name} ({StatsSystemPlugin.Singleton.Version}) than {latest}. This is a development/pre-release build and it can contain errors or bugs.",
                        ConsoleColor.DarkMagenta);
            }
            catch (TimeoutException)
            {
                LogManager.Error("Version check timed out.");
            }
            catch (Exception ex)
            {
                LogManager.Error("Version check failed.");
                LogManager.Debug($"Version check exception:\n{ex}");
            }
        });
    }

    internal static string SendLogsAsync(string content)
    {
        try
        {
            return Task.Run(async () =>
            {
                var url = $"{ApiBase}/api/v1/plugin/{Uri.EscapeDataString(StatsSystemPlugin.Singleton.Name)}/log";
                var payload = JsonSerializer.Serialize(new
                {
                    content,
                    plugin_version = StatsSystemPlugin.Singleton.Version.ToString(),
                    labapi_version = LabApiProperties.CurrentVersion
                });

                var resp = await WithTimeout(HttpQuery.PostAsync(url, payload, "application/json"));

                var (code, _) = ParseResponse(resp);
                if (code != HttpStatusCode.Created)
                {
                    LogManager.Error($"Failed to send logs: {code}");
                    return null;
                }

                var doc = JsonDocument.Parse(resp).RootElement;
                return doc.TryGetProperty("log_id", out var id) && id.ValueKind == JsonValueKind.String
                    ? id.GetString()
                    : null;
            }).GetAwaiter().GetResult();
        }
        catch (TimeoutException)
        {
            LogManager.Error("Log upload timed out.");
            return null;
        }
        catch (AggregateException ae) when (ae.InnerException != null)
        {
            LogManager.Error("Log upload failed.");
            LogManager.Debug($"Log upload exception:\n{ae.InnerException}");
            return null;
        }
        catch (Exception ex)
        {
            LogManager.Error("Log upload failed.");
            LogManager.Debug($"Log upload exception:\n{ex}");
            return null;
        }
    }

    private static async Task<string> WithTimeout(Task<string> task)
    {
        var completed = await Task.WhenAny(task, Task.Delay(RequestTimeout));
        if (completed != task)
            throw new TimeoutException();
        return await task;
    }

    private static (HttpStatusCode code, string message) ParseResponse(string json)
    {
        try
        {
            var root = JsonDocument.Parse(json).RootElement;
            var code = root.TryGetProperty("status", out var s) && s.ValueKind == JsonValueKind.Number
                ? (HttpStatusCode)s.GetInt32()
                : HttpStatusCode.InternalServerError;
            var msg = root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String
                ? m.GetString()
                : null;
            return (code, msg);
        }
        catch
        {
            return (HttpStatusCode.InternalServerError, null);
        }
    }

    private static string GetDownloadUrl(JsonElement root)
    {
        return root.TryGetProperty("download_url", out var d) && d.ValueKind == JsonValueKind.String &&
               !string.IsNullOrEmpty(d.GetString())
            ? $"Download: {d.GetString()}"
            : string.Empty;
    }
}