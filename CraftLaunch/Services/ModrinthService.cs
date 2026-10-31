using System.Text.Json;
using CraftLaunch.Models;

namespace CraftLaunch.Services;

public class ModrinthService
{
    private const string ApiBase = "https://api.modrinth.com/v2";

    public async Task<List<ModrinthProject>> SearchAsync(string query, string projectType,
        string? gameVersion, string? loader, CancellationToken ct)
    {
        var facets = new List<string> { $"[\"project_type:{projectType}\"]" };
        if (!string.IsNullOrEmpty(gameVersion))
            facets.Add($"[\"versions:{gameVersion}\"]");
        if (!string.IsNullOrEmpty(loader))
            facets.Add($"[\"categories:{loader}\"]");

        var facetsJson = "[" + string.Join(",", facets) + "]";
        var url = $"{ApiBase}/search?limit=30&index=downloads" +
                  $"&query={Uri.EscapeDataString(query ?? "")}" +
                  $"&facets={Uri.EscapeDataString(facetsJson)}";

        var json = await HttpService.Client.GetStringAsync(url, ct);
        return JsonSerializer.Deserialize<ModrinthSearchResponse>(json)?.Hits ?? new List<ModrinthProject>();
    }

    public async Task<List<ModrinthVersion>> GetVersionsAsync(string projectId, CancellationToken ct)
    {
        var json = await HttpService.Client.GetStringAsync($"{ApiBase}/project/{projectId}/version", ct);
        return JsonSerializer.Deserialize<List<ModrinthVersion>>(json) ?? new List<ModrinthVersion>();
    }
}
