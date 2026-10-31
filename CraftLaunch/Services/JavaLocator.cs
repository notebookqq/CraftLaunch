using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace CraftLaunch.Services;

public static class JavaLocator
{
    private static readonly Dictionary<string, int> VersionCache = new();

    public static List<string> FindJavaInstalls()
    {
        var results = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(string? path)
        {
            if (path is null || !File.Exists(path) || !seen.Add(path))
                return;
            results.Add(path);
        }

        var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        if (!string.IsNullOrEmpty(javaHome))
            Add(Path.Combine(javaHome, "bin", "java.exe"));

        foreach (var root in new[] { Registry.LocalMachine, Registry.CurrentUser })
        {
            foreach (var keyName in new[] { @"SOFTWARE\JavaSoft\JDK", @"SOFTWARE\JavaSoft\Java Runtime Environment" })
            {
                try
                {
                    using var key = root.OpenSubKey(keyName);
                    if (key is null) continue;
                    foreach (var version in key.GetSubKeyNames())
                    {
                        using var sub = key.OpenSubKey(version);
                        if (sub?.GetValue("JavaHome") is string home)
                            Add(Path.Combine(home, "bin", "java.exe"));
                    }
                }
                catch
                {
                    // 注册表不可读时跳过该项
                }
            }
        }

        var candidates = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
        };

        foreach (var root in candidates)
        {
            foreach (var dir in new[] { "Java", "Eclipse Adoptium", "AdoptOpenJDK", "Zulu", "Microsoft", "Amazon Corretto" })
            {
                var baseDir = Path.Combine(root, dir);
                if (!Directory.Exists(baseDir)) continue;
                foreach (var exe in Directory.EnumerateFiles(baseDir, "java.exe", SearchOption.AllDirectories))
                    Add(exe);
            }
        }

        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var exe = Path.Combine(dir, "java.exe");
            if (File.Exists(exe))
                Add(Path.GetFullPath(exe));
        }

        return results;
    }

    public static string? FindBestJava()
    {
        return FindJavaInstalls()
            .Select(java => (Path: java, Version: GetMajorVersion(java)))
            .OrderByDescending(x => x.Version)
            .Select(x => x.Path)
            .FirstOrDefault();
    }

    public static int GetMajorVersion(string javaExe)
    {
        lock (VersionCache)
        {
            if (VersionCache.TryGetValue(javaExe, out var cached))
                return cached;
        }

        var version = 0;
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = javaExe,
                Arguments = "-version",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });
            if (process is not null)
            {
                var output = process.StandardError.ReadToEnd();
                process.WaitForExit(5000);

                var start = output.IndexOf('"');
                var end = output.IndexOf('"', start + 1);
                if (start >= 0 && end > start)
                {
                    var raw = output.Substring(start + 1, end - start - 1);
                    var parts = raw.Split('.');
                    if (int.TryParse(parts[0], out var major))
                        version = major == 1 && parts.Length > 1 && int.TryParse(parts[1], out var minor) ? minor : major;
                }
            }
        }
        catch
        {
            // 无法执行时视为无效 Java
        }

        lock (VersionCache) VersionCache[javaExe] = version;
        return version;
    }
}
