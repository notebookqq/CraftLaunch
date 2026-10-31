using System.IO;
using System.IO.Compression;
using System.Text.Json;
using CraftLaunch.Models;

namespace CraftLaunch.Services;

public class ModpackInstaller
{
    private const long MaxExtractBytes = 4L * 1024 * 1024 * 1024;
    private const long MaxDownloadBytes = 8L * 1024 * 1024 * 1024;

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

        var overridesBytes = overrides.Sum(e => e.Length);
        if (overridesBytes > MaxExtractBytes)
            throw new InvalidOperationException($"整合包解压总量 {overridesBytes / 1073741824.0:0.#} GB 超过安全上限，疑似压缩炸弹，已中止");

        var downloadBytes = index.Files.Sum(f => (long)f.Filesize);
        if (downloadBytes > MaxDownloadBytes)
            throw new InvalidOperationException($"整合包文件总量 {downloadBytes / 1073741824.0:0.#} GB 超过安全上限，疑似异常整合包，已中止");

        var extracted = 0;
        foreach (var entry in overrides)
        {
            ct.ThrowIfCancellationRequested();
            string relative;
            try
            {
                relative = PathGuard.SafeJoinRelative(gameDirectory, entry.FullName["overrides/".Length..]);
            }
            catch (IOException ex)
            {
                LogManager.Instance.Warn($"跳过整合包条目 {entry.FullName}: {ex.Message}");
                continue;
            }
            var destination = Path.Combine(gameDirectory, relative);
            var destinationDir = Path.GetDirectoryName(destination);
            if (destinationDir is not null)
                Directory.CreateDirectory(destinationDir);
            entry.ExtractToFile(destination, true);
            extracted++;
            if (extracted % 50 == 0 || extracted == overrides.Count)
                Report(progress, "安装覆盖文件", relative, extracted, overrides.Count);
        }

        var items = new List<DownloadItem>();
        foreach (var f in index.Files)
        {
            if (f.Downloads.Count == 0)
                continue;
            string destination;
            try
            {
                destination = PathGuard.SafeJoin(gameDirectory, f.Path.Replace('/', Path.DirectorySeparatorChar));
            }
            catch (IOException ex)
            {
                LogManager.Instance.Warn($"跳过整合包文件 {f.Path}: {ex.Message}");
                continue;
            }
            items.Add(new DownloadItem
            {
                Url = f.Downloads[0],
                Destination = destination,
                Size = f.Filesize,
                Sha1 = f.Hashes?.Sha1,
                Display = f.Path
            });
        }

        await VersionInstaller.DownloadBatchAsync(items, "下载整合包文件", progress, ct);
        LogManager.Instance.Info($"整合包安装完成: {mrpackPath}");
    }

    private static void Report(IProgress<DownloadProgress>? progress, string phase, string file, long done, long total)
        => progress?.Report(new DownloadProgress { Phase = phase, CurrentFile = file, CompletedFiles = done, TotalFiles = total });
}
