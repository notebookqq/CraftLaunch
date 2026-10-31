using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using CraftLaunch.Models;

namespace CraftLaunch.Services;

public class DownloadItem
{
    public string Url { get; set; } = "";
    public string Destination { get; set; } = "";
    public long Size { get; set; }
    public string? Sha1 { get; set; }
    public string Display { get; set; } = "";
}

public class VersionInstaller
{
    private const int MaxParallel = 8;
    private const int MaxRetries = 4;

    public async Task InstallAsync(string gameDirectory, RemoteVersionInfo remote,
        IProgress<DownloadProgress>? progress, CancellationToken ct)
    {
        var versionId = remote.Id;
        var versionDir = Path.Combine(gameDirectory, "versions", versionId);
        var nativesDir = Path.Combine(versionDir, "natives");
        var librariesDir = Path.Combine(gameDirectory, "libraries");
        var assetsDir = Path.Combine(gameDirectory, "assets");
        Directory.CreateDirectory(nativesDir);

        Report(progress, "版本信息", versionId, 0, 1);
        var jsonText = await HttpService.Client.GetStringAsync(remote.Url, ct);
        var version = JsonSerializer.Deserialize<MojangVersionJson>(jsonText)
                      ?? throw new InvalidOperationException("版本 JSON 解析失败");
        if (!string.IsNullOrEmpty(version.InheritsFrom))
            throw new NotSupportedException($"版本 {versionId} 继承自 {version.InheritsFrom}，暂不支持继承版本，请安装其父版本后重试");
        await File.WriteAllTextAsync(Path.Combine(versionDir, $"{versionId}.json"), jsonText, ct);

        var items = new List<DownloadItem>();
        var natives = new List<DownloadItem>();
        var seenDestinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        bool AddUnique(DownloadItem item)
        {
            if (item.Url.Length == 0 || !seenDestinations.Add(item.Destination))
                return false;
            items.Add(item);
            return true;
        }

        if (version.Downloads is { } downloads && downloads.TryGetValue("client", out var client) && client.Url.Length > 0)
        {
            AddUnique(new DownloadItem
            {
                Url = client.Url,
                Destination = Path.Combine(versionDir, $"{versionId}.jar"),
                Size = client.Size,
                Sha1 = client.Sha1,
                Display = $"{versionId}.jar"
            });
        }

        foreach (var library in version.Libraries.Where(l => RuleMatcher.IsAllowed(l.Rules)))
        {
            var artifact = library.Downloads?.Artifact;
            if (artifact is { Url.Length: > 0 } && !string.IsNullOrEmpty(artifact.Path))
            {
                AddUnique(new DownloadItem
                {
                    Url = artifact.Url,
                    Destination = Path.Combine(librariesDir, artifact.Path),
                    Size = artifact.Size,
                    Sha1 = artifact.Sha1,
                    Display = artifact.Path
                });
            }

            if (library.Downloads?.Classifiers?.TryGetValue("natives-windows", out var classifier) == true &&
                classifier is { Url.Length: > 0 })
            {
                natives.Add(new DownloadItem
                {
                    Url = classifier.Url,
                    Destination = Path.Combine(librariesDir, classifier.Path ?? $"{library.Name}-natives.jar"),
                    Size = classifier.Size,
                    Sha1 = classifier.Sha1,
                    Display = classifier.Path ?? library.Name
                });
            }
        }

        natives = natives
            .GroupBy(n => n.Destination, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        string? assetIndexId = version.AssetIndex?.Id ?? version.Assets;
        string? assetIndexPath = null;
        if (version.AssetIndex is { Url.Length: > 0 } assetIndex)
        {
            assetIndexPath = Path.Combine(assetsDir, "indexes", $"{assetIndex.Id}.json");
            AddUnique(new DownloadItem
            {
                Url = assetIndex.Url,
                Destination = assetIndexPath,
                Display = $"资源索引 {assetIndex.Id}"
            });
        }

        await DownloadBatchAsync(items, "下载游戏文件", progress, ct);
        await DownloadBatchAsync(natives, "下载本地库", progress, ct);
        ExtractNatives(natives, nativesDir);

        if (assetIndexPath is not null && File.Exists(assetIndexPath))
        {
            var assetItems = ParseAssetObjects(assetIndexPath)
                .Select(o => new DownloadItem
                {
                    Url = $"https://resources.download.minecraft.net/{o.Hash[..2]}/{o.Hash}",
                    Destination = Path.Combine(assetsDir, "objects", o.Hash[..2], o.Hash),
                    Size = o.Size,
                    Sha1 = o.Hash,
                    Display = o.Hash
                })
                .ToList();
            await DownloadBatchAsync(assetItems, "下载资源文件", progress, ct);
        }

        Report(progress, "完成", versionId, 1, 1);
        LogManager.Instance.Info($"版本 {versionId} 安装完成（{versionDir}）");
    }

    public static async Task DownloadBatchAsync(List<DownloadItem> items, string phase,
        IProgress<DownloadProgress>? progress, CancellationToken ct)
    {
        if (items.Count == 0)
            return;

        var total = items.Count;
        var done = 0;
        var failures = new List<string>();

        await Parallel.ForEachAsync(items, new ParallelOptions { MaxDegreeOfParallelism = MaxParallel, CancellationToken = ct },
            async (item, token) =>
            {
                try
                {
                    await DownloadFileAsync(item, token);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lock (failures)
                    {
                        failures.Add($"{item.Display}: {ex.Message}");
                    }
                }
                finally
                {
                    var count = Interlocked.Increment(ref done);
                    if (count % 20 == 0 || count == total)
                        Report(progress, phase, item.Display, count, total);
                }
            });

        if (failures.Count > 0)
            throw new InvalidOperationException(
                $"有 {failures.Count} 个文件下载失败: {string.Join("; ", failures.Take(5))}" +
                (failures.Count > 5 ? " 等" : "") + "。可重新点击下载，已完成的文件会自动跳过。");
    }

    public static async Task DownloadFileAsync(DownloadItem item, CancellationToken ct)
    {
        var destinationDir = Path.GetDirectoryName(item.Destination);
        if (destinationDir is not null)
            Directory.CreateDirectory(destinationDir);

        if (File.Exists(item.Destination) && (item.Size <= 0 || new FileInfo(item.Destination).Length == item.Size))
            return;

        for (var attempt = 1; ; attempt++)
        {
            var tempPath = $"{item.Destination}.{Guid.NewGuid():N}.download";
            try
            {
                using var response = await HttpService.Client.GetAsync(item.Url, HttpCompletionOption.ResponseHeadersRead, ct);
                response.EnsureSuccessStatusCode();
                await using var httpStream = await response.Content.ReadAsStreamAsync(ct);
                string actualHash;
                await using (var fileStream = File.Create(tempPath))
                {
                    using var hasher = SHA1.Create();
                    var buffer = new byte[81920];
                    int read;
                    while ((read = await httpStream.ReadAsync(buffer, ct)) > 0)
                    {
                        hasher.TransformBlock(buffer, 0, read, null, 0);
                        await fileStream.WriteAsync(buffer.AsMemory(0, read), ct);
                    }
                    hasher.TransformFinalBlock([], 0, 0);
                    actualHash = Convert.ToHexString(hasher.Hash!);
                    await fileStream.FlushAsync(ct);
                }

                if (item.Sha1 is not null &&
                    !actualHash.Equals(item.Sha1, StringComparison.OrdinalIgnoreCase))
                    throw new IOException($"文件校验失败: {item.Display}");

                File.Move(tempPath, item.Destination, true);
                return;
            }
            catch (OperationCanceledException)
            {
                TryDelete(tempPath);
                throw;
            }
            catch when (attempt < MaxRetries)
            {
                TryDelete(tempPath);
                await Task.Delay(500 * attempt, ct);
            }
            catch
            {
                TryDelete(tempPath);
                throw;
            }
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // 临时文件被占用时留给下次清理
        }
    }

    private static void ExtractNatives(List<DownloadItem> natives, string nativesDir)
    {
        foreach (var item in natives)
        {
            if (!File.Exists(item.Destination))
                continue;
            using var archive = ZipFile.OpenRead(item.Destination);
            foreach (var entry in archive.Entries)
            {
                if (entry.FullName.EndsWith("/") || entry.FullName.StartsWith("META-INF"))
                    continue;
                entry.ExtractToFile(Path.Combine(nativesDir, entry.Name), true);
            }
        }
    }

    private static List<(string Hash, long Size)> ParseAssetObjects(string indexPath)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(indexPath));
        var result = new List<(string, long)>();

        if (doc.RootElement.TryGetProperty("objects", out var objects) && objects.ValueKind == JsonValueKind.Object)
        {
            foreach (var obj in objects.EnumerateObject())
            {
                var hash = obj.Value.GetProperty("hash").GetString();
                if (string.IsNullOrEmpty(hash) || hash.Length != 40)
                    continue;
                var size = obj.Value.TryGetProperty("size", out var sizeElement) ? sizeElement.GetInt64() : 0;
                result.Add((hash, size));
            }
        }
        return result;
    }

    private static void Report(IProgress<DownloadProgress>? progress, string phase, string file, long done, long total)
        => progress?.Report(new DownloadProgress { Phase = phase, CurrentFile = file, CompletedFiles = done, TotalFiles = total });
}
