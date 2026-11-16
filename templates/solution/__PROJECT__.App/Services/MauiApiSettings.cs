using System.Text.Json;

using Microsoft.Maui.Storage;

namespace __PROJECT__.App.Services;

internal static class MauiApiSettings
{
    private const string ConfigFileName = "appsettings.json";
    private const string PreferenceKey = "ApiBaseUrl";

    public static string ResolveApiBaseUrl(Func<HttpClientHandler> handlerFactory)
    {
        var config = LoadConfig();
        var candidates = new List<string>();

        AddCandidate(candidates, Preferences.Default.Get(PreferenceKey, string.Empty));
        AddCandidate(candidates, config.ApiBaseUrl);

        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            AddCandidate(candidates, "https://10.0.2.2:44325");
            AddCandidate(candidates, "https://10.0.2.2:44333");
            AddCandidate(candidates, "https://10.0.2.2:7143");
            AddCandidate(candidates, "http://10.0.2.2:60453");
            AddCandidate(candidates, "http://10.0.2.2:7142");
        }

        AddCandidate(candidates, "https://localhost:44325");
        AddCandidate(candidates, "https://127.0.0.1:44325");
        AddCandidate(candidates, "https://localhost:44333");
        AddCandidate(candidates, "https://127.0.0.1:44333");
        AddCandidate(candidates, "https://localhost:7143");
        AddCandidate(candidates, "https://127.0.0.1:7143");
        AddCandidate(candidates, "http://localhost:60453");
        AddCandidate(candidates, "http://127.0.0.1:60453");
        AddCandidate(candidates, "http://localhost:7142");
        AddCandidate(candidates, "http://127.0.0.1:7142");
        foreach (var fallbackUrl in config.ApiFallbackUrls)
        {
            AddCandidate(candidates, fallbackUrl);
        }

        foreach (var candidate in candidates)
        {
            if (Probe(candidate, handlerFactory))
            {
                System.Diagnostics.Debug.WriteLine($"[MauiApiSettings] API base URL: {candidate}");
                return candidate;
            }
        }

        var fallback = candidates.FirstOrDefault() ?? "https://localhost:7143";
        System.Diagnostics.Debug.WriteLine($"[MauiApiSettings] No reachable API; using: {fallback}");
        return fallback;
    }

    private static bool Probe(string baseUrl, Func<HttpClientHandler> handlerFactory)
    {
        try
        {
            using var handler = handlerFactory();
            using var client = new HttpClient(handler)
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(2),
            };
            using var response = client.GetAsync("health").GetAwaiter().GetResult();
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static ApiSettings LoadConfig()
    {
        try
        {
            using var stream = FileSystem.OpenAppPackageFileAsync(ConfigFileName).GetAwaiter().GetResult();
            var settings = JsonSerializer.Deserialize<ApiSettings>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
            return settings ?? new ApiSettings();
        }
        catch
        {
            return new ApiSettings();
        }
    }

    private static void AddCandidate(List<string> candidates, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return;
        }

        var normalized = value.Trim().TrimEnd('/');
        if (!candidates.Contains(normalized, StringComparer.OrdinalIgnoreCase))
        {
            candidates.Add(normalized);
        }
    }

    private sealed class ApiSettings
    {
        public string? ApiBaseUrl { get; set; }
        public string[] ApiFallbackUrls { get; set; } = Array.Empty<string>();
    }
}
