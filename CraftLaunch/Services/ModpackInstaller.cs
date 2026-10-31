using System.IO;
using System.IO.Compression;
using System.Text.Json;
using CraftLaunch.Models;

namespace CraftLaunch.Services;

public class ModpackInstaller
{
    public async Task InstallAsync(string mrpackPath, string gameDirectory,
        IProgress<DownloadProgress>? progress, CancellationToken ct)
    {
        using var archive = ZipFile.OpenRead(mrpackPath);
        var indexEntry = archive.GetEntry("modrinth.index.json")
                         ?? throw new InvalidOperationException("无效的 .mrpack 文件：缺少 modrinth.index.json");

        ModrinthIndex index;
        await using (var stream = indexEntry.Open())
        {
            index = JsonSerializer.Deserialize<ModrinthIndex>(stream)
                    ?? throw new InvalidOperationException("整合包索引解析失败");
        }

        if (index.Dependencies is { } dependencies && dependencies.TryGetValue("minecraft", out var mcVersion))
            LogManager.Instance.Info($"整合包目标 Minecraft 版本: {mcVersion}");

        var overrides = archive.Entries
            .Where(e => e.FullName.StartsWith("overrides/") && e.Name.Length > 0)
            .ToList();

        var extracted = 0;
        foreach (var entry in overrides)
        {
            ct.ThrowIfCancellationRequested();
            var relative = entry.FullName["overrides/".Length..];
            var destination = Path.Combine(gameDirectory, relative.Replace('/', Path.DirectorySeparatorChar));
            var destinationDir = Path.GetDirectoryName(destination);
            if (destinationDir is not null)
                Directory.CreateDirectory(destinationDir);
            entry.ExtractToFile(destination, true);
            extracted++;
            if (extracted % 50 == 0 || extracted == overrides.Count)
                Report(progress, "安装覆盖文件", relative, extracted, overrides.Count);
        }

        var items = index.Files
            .Where(f => f.Downloads.Count > 0)
            .Select(f => new DownloadItem
            {
                Url = f.Downloads[0],
                Destination = Path.Combine(gameDirectory, f.Path.Replace('/', Path.DirectorySeparatorChar)),
                Size = f.Filesize,
                Sha1 = f.Hashes?.Sha1,
                Display = f.Path
            })
            .ToList();

        await VersionInstaller.DownloadBatchAsync(items, "下载整合包文件", progress, ct);
        LogManager.Instance.Info($"整合包安装完成: {mrpackPath}");
    }

    private static void Report(IProgress<DownloadProgress>? progress, string phase, string file, long done, long total)
        => progress?.Report(new DownloadProgress { Phase = phase, CurrentFile = file, CompletedFiles = done, TotalFiles = total });
}
