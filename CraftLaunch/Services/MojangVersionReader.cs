using System.IO;
using System.Text.Json;
using CraftLaunch.Models;

namespace CraftLaunch.Services;

public static class MojangVersionReader
{
    public static MojangVersionJson Load(string jsonPath)
    {
        return JsonSerializer.Deserialize<MojangVersionJson>(File.ReadAllText(jsonPath))
               ?? throw new InvalidOperationException($"版本 JSON 解析失败: {jsonPath}");
    }

    public static int GetRequiredJavaMajorVersion(McVersion version)
    {
        try
        {
            if (string.IsNullOrEmpty(version.JsonPath) || !File.Exists(version.JsonPath))
                return 0;
            using var doc = JsonDocument.Parse(File.ReadAllText(version.JsonPath));
            if (doc.RootElement.TryGetProperty("javaVersion", out var javaVersion) &&
                javaVersion.TryGetProperty("majorVersion", out var major) &&
                major.TryGetInt32(out var value))
                return value;
        }
        catch
        {
            // 无法解析时不做 Java 版本限制
        }
        return 0;
    }
}
