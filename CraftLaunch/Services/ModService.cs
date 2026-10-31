using System.IO;
using CraftLaunch.Models;

namespace CraftLaunch.Services;

public class ModService : IModProvider
{
    public Task<List<ModInfo>> GetModsAsync(string gameDirectory)
        => GetContentAsync(gameDirectory, "mods", "*.jar");

    public Task<List<ModInfo>> GetShadersAsync(string gameDirectory)
        => GetContentAsync(gameDirectory, "shaderpacks", "*.zip");

    public Task<List<ModInfo>> GetContentAsync(string gameDirectory, string subFolder, params string[] patterns)
        => Task.Run(() =>
        {
            var dir = Path.Combine(gameDirectory, subFolder);
            if (!Directory.Exists(dir))
                return new List<ModInfo>();

            return patterns
                .SelectMany(pattern => Directory.EnumerateFiles(dir, pattern))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(f => new FileInfo(f))
                .Select(fi => new ModInfo
                {
                    Name = fi.Name,
                    Path = fi.FullName,
                    SizeMB = Math.Round(fi.Length / 1048576.0, 1)
                })
                .OrderBy(m => m.Name)
                .ToList();
        });
}
