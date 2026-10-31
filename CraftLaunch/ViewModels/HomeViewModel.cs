using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using CraftLaunch.Models;
using CraftLaunch.Services;
using Microsoft.Win32;

namespace CraftLaunch.ViewModels;

public class HomeViewModel : ViewModelBase
{
    private readonly IVersionProvider _versionProvider = new VersionService();
    private readonly GameLauncher _launcher = new();
    private readonly StringBuilder _logBuilder = new();

    public ObservableCollection<McVersion> Versions { get; } = new();

    private McVersion? _selectedVersion;

    public McVersion? SelectedVersion
    {
        get => _selectedVersion;
        set => Set(ref _selectedVersion, value);
    }

    private string _gamePath;

    public string GamePath
    {
        get => _gamePath;
        set
        {
            if (!Set(ref _gamePath, value))
                return;
            ConfigService.Current.GameDirectory = value;
            ConfigService.Save();
            RefreshVersions();
        }
    }

    private double _memoryMB;

    public double MemoryMB
    {
        get => _memoryMB;
        set
        {
            if (Set(ref _memoryMB, value))
            {
                OnPropertyChanged(nameof(MemoryText));
                ConfigService.Current.MemoryMB = (int)value;
            }
        }
    }

    public string MemoryText => $"{MemoryMB / 1024.0:0.#} GB";

    private string _logText = "";

    public string LogText
    {
        get => _logText;
        private set => Set(ref _logText, value);
    }

    private bool _isGameRunning;

    public bool IsGameRunning
    {
        get => _isGameRunning;
        private set
        {
            if (Set(ref _isGameRunning, value))
            {
                OnPropertyChanged(nameof(StatusText));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    private bool _isBusy;

    public bool IsBusy
    {
        get => _isBusy;
        private set => Set(ref _isBusy, value);
    }

    public string StatusText => IsGameRunning ? "游戏运行中" : (IsBusy ? "正在启动..." : "空闲");

    public RelayCommand LaunchCommand { get; }
    public RelayCommand StopCommand { get; }
    public RelayCommand BrowseGamePathCommand { get; }
    public RelayCommand RefreshCommand { get; }

    public HomeViewModel()
    {
        _gamePath = ConfigService.Current.GameDirectory;
        MemoryMB = ConfigService.Current.MemoryMB;

        LaunchCommand = new RelayCommand(async _ => await LaunchAsync(), _ => !IsBusy && !IsGameRunning && SelectedVersion is not null);
        StopCommand = new RelayCommand(_ => StopGame(), _ => IsGameRunning);
        BrowseGamePathCommand = new RelayCommand(_ => BrowseGamePath());
        RefreshCommand = new RelayCommand(_ => RefreshVersions());

        LogManager.Instance.LogEmitted += AppendLog;
        _launcher.OutputReceived += line => LogManager.Instance.Game(line);
        _launcher.GameExited += code => Application.Current.Dispatcher.BeginInvoke(() =>
        {
            IsGameRunning = false;
            LogManager.Instance.Info($"游戏进程已退出，退出码: {code}");
        });

        RefreshVersions();
    }

    public void RefreshVersions()
    {
        var gamePath = ConfigService.Current.GameDirectory;
        _versionProvider.GetVersionsAsync(gamePath).ContinueWith(t =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Versions.Clear();
                if (t.IsFaulted)
                {
                    LogManager.Instance.Error($"版本扫描失败: {t.Exception?.GetBaseException().Message}");
                    return;
                }
                foreach (var version in t.Result)
                    Versions.Add(version);
                if (SelectedVersion is null && Versions.Count > 0)
                    SelectedVersion = Versions[0];
                LogManager.Instance.Info($"已加载 {Versions.Count} 个本地版本");
            });
        });
    }

    private async Task LaunchAsync()
    {
        if (SelectedVersion is null)
            return;

        var javaPath = ResolveJavaPath();
        if (string.IsNullOrEmpty(javaPath))
        {
            LogManager.Instance.Error("未找到可用的 Java，请在设置页面配置 Java 路径");
            return;
        }

        var requiredJava = MojangVersionReader.GetRequiredJavaMajorVersion(SelectedVersion);
        var actualJava = JavaLocator.GetMajorVersion(javaPath);
        if (requiredJava > 0 && actualJava > 0 && actualJava < requiredJava)
        {
            LogManager.Instance.Error($"启动该版本需要 Java {requiredJava}+，当前为 Java {actualJava}，请在设置中更换 Java 路径");
            return;
        }

        try
        {
            IsBusy = true;
            var context = new LaunchContext
            {
                Version = SelectedVersion,
                JavaPath = javaPath,
                GameDirectory = ConfigService.Current.GameDirectory,
                MemoryMB = (int)MemoryMB,
                Proxy = ProxySetting.From(ConfigService.Current)
            };
            LogManager.Instance.Info($"正在启动 Minecraft {SelectedVersion.Id}（内存 {MemoryMB:0} MB）...");
            await _launcher.LaunchAsync(context);
            IsGameRunning = true;
        }
        catch (Exception ex)
        {
            LogManager.Instance.Error($"启动失败: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void StopGame()
    {
        LogManager.Instance.Warn("正在结束由 CraftLaunch 启动的游戏进程（不影响其他程序）...");
        _launcher.StopAll();
        IsGameRunning = false;
    }

    private string? ResolveJavaPath()
    {
        var configured = ConfigService.Current.JavaPath;
        if (!string.IsNullOrEmpty(configured) && File.Exists(configured))
            return configured;

        var best = JavaLocator.FindBestJava();
        if (best is not null)
            LogManager.Instance.Info($"未配置 Java，自动选用: {best}");
        return best;
    }

    private void BrowseGamePath()
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择 .minecraft 游戏目录",
            CheckFileExists = false,
            CheckPathExists = false,
            ValidateNames = false,
            FileName = "选择此文件夹",
            InitialDirectory = Directory.Exists(GamePath) ? GamePath : null
        };
        if (dialog.ShowDialog() == true && Path.GetDirectoryName(dialog.FileName) is { } folder)
            GamePath = folder;
    }

    private void AppendLog(LogLevel level, string line)
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            _logBuilder.AppendLine(line);
            if (_logBuilder.Length > 200_000)
                _logBuilder.Remove(0, _logBuilder.Length - 100_000);
            LogText = _logBuilder.ToString();
        });
    }
}
