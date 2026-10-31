using System.Text.Json;
using System.Text.Json.Serialization;

namespace CraftLaunch.Models;

public class MojangVersionJson
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("mainClass")]
    public string MainClass { get; set; } = "";

    [JsonPropertyName("minecraftArguments")]
    public string? MinecraftArguments { get; set; }

    [JsonPropertyName("assets")]
    public string? Assets { get; set; }

    [JsonPropertyName("assetIndex")]
    public MojangAssetIndexRef? AssetIndex { get; set; }

    [JsonPropertyName("downloads")]
    public Dictionary<string, MojangDownloadableFile>? Downloads { get; set; }

    [JsonPropertyName("libraries")]
    public List<MojangLibrary> Libraries { get; set; } = new();

    [JsonPropertyName("arguments")]
    public MojangArguments? Arguments { get; set; }

    [JsonPropertyName("javaVersion")]
    public MojangJavaVersion? JavaVersion { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("inheritsFrom")]
    public string? InheritsFrom { get; set; }
}

public class MojangAssetIndexRef
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";
}

public class MojangDownloadableFile
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("sha1")]
    public string? Sha1 { get; set; }
}

public class MojangLibrary
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("rules")]
    public List<MojangRule>? Rules { get; set; }

    [JsonPropertyName("downloads")]
    public MojangLibraryDownloads? Downloads { get; set; }
}

public class MojangLibraryDownloads
{
    [JsonPropertyName("artifact")]
    public MojangDownloadableFile? Artifact { get; set; }

    [JsonPropertyName("classifiers")]
    public Dictionary<string, MojangDownloadableFile>? Classifiers { get; set; }
}

public class MojangRule
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = "";

    [JsonPropertyName("os")]
    public MojangOsRule? Os { get; set; }

    [JsonPropertyName("features")]
    public Dictionary<string, JsonElement>? Features { get; set; }
}

public class MojangOsRule
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("arch")]
    public string? Arch { get; set; }
}

public class MojangArguments
{
    [JsonPropertyName("game")]
    public List<JsonElement>? Game { get; set; }

    [JsonPropertyName("jvm")]
    public List<JsonElement>? Jvm { get; set; }
}

public class MojangArgumentValue
{
    [JsonPropertyName("rules")]
    public List<MojangRule>? Rules { get; set; }

    [JsonPropertyName("value")]
    public JsonElement Value { get; set; }
}

public class MojangJavaVersion
{
    [JsonPropertyName("majorVersion")]
    public int MajorVersion { get; set; }

    [JsonPropertyName("component")]
    public string? Component { get; set; }
}
