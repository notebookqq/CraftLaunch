using System.Diagnostics;
using CraftLaunch.Models;

namespace CraftLaunch.Services;

public class GameLauncher
{
    private static readonly object Lock = new();
    private static readonly List<Process> Tracked = new();

    public event Action<string>? OutputReceived;
    public event Action<int>? GameExited;

    public bool IsRunning
    {
        get { lock (Lock) return Tracked.Any(p => !p.HasExited); }
    }

    public async Task LaunchAsync(LaunchContext context)
    {
        var arguments = new LaunchArgumentBuilder().Build(context);

        var startInfo = new ProcessStartInfo
        {
            FileName = context.JavaPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = context.GameDirectory
        };

        var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) OutputReceived?.Invoke(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) OutputReceived?.Invoke(e.Data); };
        process.Exited += (_, _) =>
        {
            lock (Lock) Tracked.Remove(process);
            GameExited?.Invoke(process.ExitCode);
        };

        if (!process.Start())
            throw new InvalidOperationException("无法启动 Java 进程");

        lock (Lock) Tracked.Add(process);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await Task.CompletedTask;
    }

    public void StopAll()
    {
        List<Process> toKill;
        lock (Lock)
        {
            toKill = Tracked.Where(p => !p.HasExited).ToList();
            Tracked.Clear();
        }

        foreach (var process in toKill)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // 进程可能已自行退出
            }
        }
    }
}
