using System.IO;
using System.Text.Json;
using CraftLaunch.Models;

namespace CraftLaunch.Services;

public class RemoteVersionService
{
    private const string ManifestUrl = "https://piston-meta.mojang.com/mc/game/version_manifest_v2.json";
    private const int CacheMinutes = 30;

    private static readonly object Lock = new();
    private static List<RemoteVersionInfo>? _cache;
    private static DateTime _cacheTime;

    public async Task<List<RemoteVersionInfo>> GetVersionsAsync()
    {
        lock (Lock)
        {
            if (_cache is not null && DateTime.UtcNow - _cacheTime < TimeSpan.FromMinutes(CacheMinutes))
                return _cache;
        }

        var json = await HttpService.Client.GetStringAsync(ManifestUrl);
        using var doc = JsonDocument.Parse(json);

        var versions = new List<RemoteVersionInfo>();
        if (doc.RootElement.TryGetProperty("versions", out var items) &&
            items.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in items.EnumerateArray())
            {
                var id = item.GetProperty("id").GetString();
                var url = item.GetProperty("url").GetString();
                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(url))
                    continue;
                versions.Add(new RemoteVersionInfo
                {
                    Id = id,
                    Type = item.GetProperty("type").GetString() ?? "release",
                    Url = url,
                    ReleaseTime = item.TryGetProperty("releaseTime", out var t) &&
                                  t.TryGetDateTime(out var dt) ? dt : default
                });
            }
        }

        lock (Lock)
        {
            _cache = versions;
            _cacheTime = DateTime.UtcNow;
        }
        return versions;
    }
}
