using System.IO;
using CraftLaunch.Models;

namespace CraftLaunch.Services;

public class VersionService : IVersionProvider
{
    public Task<List<McVersion>> GetVersionsAsync(string gameDirectory)
        => Task.Run(() =>
        {
            var root = Path.Combine(gameDirectory, "versions");
            var versions = new List<McVersion>();

            if (!Directory.Exists(root))
                return versions;

            foreach (var sub in Directory.EnumerateDirectories(root))
            {
                var id = Path.GetFileName(sub);
                var jsonPath = Path.Combine(sub, $"{id}.json");
                var jarPath = Path.Combine(sub, $"{id}.jar");

                if (!File.Exists(jsonPath) && !File.Exists(jarPath))
                    continue;

                versions.Add(new McVersion
                {
                    Id = id,
                    Directory = sub,
                    JsonPath = File.Exists(jsonPath) ? jsonPath : "",
                    JarPath = File.Exists(jarPath) ? jarPath : "",
                    Type = GuessType(id),
                    ModifiedTime = Directory.GetLastWriteTime(sub)
                });
            }

            return versions.OrderByDescending(v => v.ModifiedTime).ToList();
        });

    public void Delete(McVersion version)
    {
        if (Directory.Exists(version.Directory))
            Directory.Delete(version.Directory, true);
    }

    private static string GuessType(string id)
    {
        var lower = id.ToLowerInvariant();
        if (lower.Contains("fabric")) return "Fabric";
        if (lower.Contains("forge")) return "Forge";
        if (lower.Contains("neoforge") || lower.Contains("neoform")) return "NeoForge";
        if (lower.Contains("quilt")) return "Quilt";
        if (lower.Contains("optifine")) return "OptiFine";
        return "Release";
    }
}
