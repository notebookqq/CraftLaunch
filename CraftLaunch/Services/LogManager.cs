using System.IO;

namespace CraftLaunch.Services;

public enum LogLevel
{
    Info,
    Warn,
    Error,
    Game
}

public sealed class LogManager
{
    public static LogManager Instance { get; } = new();

    public event Action<LogLevel, string>? LogEmitted;

    private readonly object _lock = new();
    private readonly string _logDirectory;

    private LogManager()
    {
        _logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CraftLaunch", "logs");
        Directory.CreateDirectory(_logDirectory);
    }

    public void Info(string message) => Write(LogLevel.Info, message);
    public void Warn(string message) => Write(LogLevel.Warn, message);
    public void Error(string message) => Write(LogLevel.Error, message);
    public void Game(string message) => Write(LogLevel.Game, message);

    public void Write(LogLevel level, string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] [{level,-5}] {message}";
        lock (_lock)
        {
            try
            {
                var file = Path.Combine(_logDirectory, $"{DateTime.Now:yyyyMMdd}.log");
                File.AppendAllText(file, line + Environment.NewLine);
            }
            catch
            {
                // 日志写盘失败不影响 UI 运行
            }
        }
        LogEmitted?.Invoke(level, line);
    }
}
