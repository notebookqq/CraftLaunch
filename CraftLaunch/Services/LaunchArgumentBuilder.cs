using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CraftLaunch.Models;

namespace CraftLaunch.Services;

public partial class LaunchArgumentBuilder : ILaunchArgumentBuilder
{
    [GeneratedRegex(@"\$\{([^}]+)\}")]
    private static partial Regex PlaceholderRegex();

    public string Build(LaunchContext context)
    {
        if (string.IsNullOrEmpty(context.Version.JsonPath) || !File.Exists(context.Version.JsonPath))
            throw new InvalidOperationException(
                "版本 JSON 不存在，无法构造启动参数。请先在版本管理页面下载完整版本，或放入官方版本文件。");

        var version = MojangVersionReader.Load(context.Version.JsonPath);
        var nativesDir = Path.Combine(context.Version.Directory, "natives");
        var librariesDir = Path.Combine(context.GameDirectory, "libraries");
        var assetsRoot = Path.Combine(context.GameDirectory, "assets");

        var classpath = BuildClasspath(version, librariesDir, context.Version.JarPath);

        var missing = classpath.Where(p => !File.Exists(p)).ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"版本依赖不完整，缺少 {missing.Count} 个库文件（如 {Path.GetFileName(missing[0])}）。" +
                "请在下载页重新下载该版本，已下载的文件会自动跳过。");

        var substitutions = BuildSubstitutions(context, version, nativesDir, assetsRoot, classpath);

        var args = new List<string>
        {
            $"-Xmx{context.MemoryMB}M",
            $"-Xms{Math.Min(1024, context.MemoryMB)}M",
            $"-Djava.library.path={nativesDir}"
        };

        if (context.Proxy is { Enabled: true } proxy)
        {
            args.Add($"-Dhttp.proxyHost={proxy.Host}");
            args.Add($"-Dhttp.proxyPort={proxy.Port}");
            args.Add($"-Dhttps.proxyHost={proxy.Host}");
            args.Add($"-Dhttps.proxyPort={proxy.Port}");
        }

        AppendArguments(version.Arguments?.Jvm, substitutions, args);

        var gameArgs = new List<string>();
        if (version.Arguments?.Game is { Count: > 0 })
        {
            AppendArguments(version.Arguments.Game, substitutions, gameArgs);
        }
        else if (!string.IsNullOrEmpty(version.MinecraftArguments))
        {
            gameArgs.AddRange(version.MinecraftArguments
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(a => Substitute(a, substitutions)));
        }
        else
        {
            throw new InvalidOperationException("版本 JSON 中缺少启动参数定义（arguments / minecraftArguments）");
        }

        args.Add("-cp");
        args.Add(string.Join(Path.PathSeparator, classpath));
        args.Add(version.MainClass);
        args.AddRange(gameArgs);

        return JoinArgs(args);
    }

    private static List<string> BuildClasspath(MojangVersionJson version, string librariesDir, string jarPath)
    {
        var classpath = new List<string>();
        foreach (var library in version.Libraries)
        {
            if (!RuleMatcher.IsAllowed(library.Rules))
                continue;

            var artifact = library.Downloads?.Artifact;
            if (!string.IsNullOrEmpty(artifact?.Path))
                classpath.Add(Path.Combine(librariesDir, artifact.Path));

            if (library.Downloads?.Classifiers?.TryGetValue("natives-windows", out var natives) == true &&
                !string.IsNullOrEmpty(natives.Path))
                classpath.Add(Path.Combine(librariesDir, natives.Path));
        }

        if (File.Exists(jarPath))
            classpath.Add(jarPath);
        return classpath;
    }

    private static Dictionary<string, string> BuildSubstitutions(LaunchContext context, MojangVersionJson version,
        string nativesDir, string assetsRoot, List<string> classpath)
    {
        return new Dictionary<string, string>
        {
            ["auth_player_name"] = "Player",
            ["auth_uuid"] = Guid.NewGuid().ToString("N"),
            ["auth_access_token"] = "0",
            ["auth_session"] = "token:0:0",
            ["auth_xuid"] = "0",
            ["clientid"] = "0",
            ["user_type"] = "msa",
            ["user_properties"] = "{}",
            ["version_name"] = context.Version.Id,
            ["version_type"] = version.Type ?? "",
            ["game_directory"] = context.GameDirectory,
            ["assets_root"] = assetsRoot,
            ["game_assets"] = assetsRoot,
            ["assets_index_name"] = version.AssetIndex?.Id ?? version.Assets ?? "legacy",
            ["natives_directory"] = nativesDir,
            ["launcher_name"] = "CraftLaunch",
            ["launcher_version"] = "0.1.0",
            ["classpath"] = string.Join(Path.PathSeparator, classpath),
            ["classpath_separator"] = Path.PathSeparator.ToString(),
            ["resolution_width"] = "1280",
            ["resolution_height"] = "720",
            ["os_name"] = "windows",
            ["os_arch"] = Environment.Is64BitOperatingSystem ? "x64" : "x86",
            ["os_version"] = Environment.OSVersion.Version.ToString(),
            ["java_library_path"] = nativesDir
        };
    }

    private static void AppendArguments(List<JsonElement>? tokens, Dictionary<string, string> substitutions,
        List<string> output)
    {
        if (tokens is null)
            return;

        foreach (var token in tokens)
        {
            if (token.ValueKind == JsonValueKind.String)
            {
                output.Add(Substitute(token.GetString() ?? "", substitutions));
            }
            else if (token.ValueKind == JsonValueKind.Object)
            {
                var argument = token.Deserialize<MojangArgumentValue>();
                if (argument is null || !RuleMatcher.IsAllowed(argument.Rules))
                    continue;

                if (argument.Value.ValueKind == JsonValueKind.String)
                {
                    output.Add(Substitute(argument.Value.GetString() ?? "", substitutions));
                }
                else if (argument.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var value in argument.Value.EnumerateArray())
                    {
                        if (value.ValueKind == JsonValueKind.String)
                            output.Add(Substitute(value.GetString() ?? "", substitutions));
                    }
                }
            }
        }
    }

    private static string Substitute(string template, Dictionary<string, string> substitutions)
        => PlaceholderRegex().Replace(template, match =>
            substitutions.TryGetValue(match.Groups[1].Value, out var value) ? value : "");

    private static string JoinArgs(IEnumerable<string> args)
        => string.Join(' ', args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a));
}
