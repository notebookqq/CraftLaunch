using System.Text.Json.Serialization;

namespace CraftLaunch.Models;

public class ModrinthProject
{
    [JsonPropertyName("project_id")]
    public string ProjectId { get; set; } = "";

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = "";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("downloads")]
    public long Downloads { get; set; }

    [JsonPropertyName("icon_url")]
    public string? IconUrl { get; set; }

    public string DownloadsLabel => Downloads switch
    {
        >= 1_000_000 => $"{Downloads / 1_000_000.0:0.#}M",
        >= 1_000 => $"{Downloads / 1_000.0:0.#}K",
        _ => Downloads.ToString()
    };
}

public class ModrinthSearchResponse
{
    [JsonPropertyName("hits")]
    public List<ModrinthProject> Hits { get; set; } = new();
}

public class ModrinthVersion
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("version_number")]
    public string VersionNumber { get; set; } = "";

    [JsonPropertyName("game_versions")]
    public List<string> GameVersions { get; set; } = new();

    [JsonPropertyName("loaders")]
    public List<string> Loaders { get; set; } = new();

    [JsonPropertyName("date_published")]
    public DateTime DatePublished { get; set; }

    [JsonPropertyName("files")]
    public List<ModrinthFile> Files { get; set; } = new();
}

public class ModrinthFile
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("filename")]
    public string Filename { get; set; } = "";

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("primary")]
    public bool Primary { get; set; }
}

public class ModrinthIndex
{
    [JsonPropertyName("game")]
    public string Game { get; set; } = "minecraft";

    [JsonPropertyName("files")]
    public List<ModrinthIndexFile> Files { get; set; } = new();

    [JsonPropertyName("dependencies")]
    public Dictionary<string, string>? Dependencies { get; set; }
}

public class ModrinthIndexFile
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = "";

    [JsonPropertyName("downloads")]
    public List<string> Downloads { get; set; } = new();

    [JsonPropertyName("filesize")]
    public long Filesize { get; set; }

    [JsonPropertyName("hashes")]
    public ModrinthHashes? Hashes { get; set; }
}

public class ModrinthHashes
{
    [JsonPropertyName("sha1")]
    public string? Sha1 { get; set; }
}
