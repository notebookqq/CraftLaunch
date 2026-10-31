using System.IO;
using System.Windows.Input;
using CraftLaunch.Services;

namespace CraftLaunch.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private bool _isDarkTheme = ConfigService.Current.Theme == "Dark";
    private string _gameDirectory = ConfigService.Current.GameDirectory;
    private string _javaPath = ConfigService.Current.JavaPath;
    private string _javaStatus = "";
    private bool _proxyEnabled = ConfigService.Current.ProxyEnabled;
    private string _proxyHost = ConfigService.Current.ProxyHost;
    private string _proxyPort = ConfigService.Current.ProxyPort.ToString();
    private bool _isDetecting;

    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (!Set(ref _isDarkTheme, value))
                return;
            var theme = value ? "Dark" : "Light";
            ThemeManager.Apply(theme);
            ConfigService.Current.Theme = theme;
            ConfigService.Save();
            LogManager.Instance.Info($"界面主题已切换为 {theme}");
        }
    }

    public string GameDirectory
    {
        get => _gameDirectory;
        set => Set(ref _gameDirectory, value);
    }

    public string JavaPath
    {
        get => _javaPath;
        set
        {
            if (Set(ref _javaPath, value))
                _javaStatus = "";
        }
    }

    public string JavaStatus
    {
        get => _javaStatus;
        private set => Set(ref _javaStatus, value);
    }

    public bool ProxyEnabled
    {
        get => _proxyEnabled;
        set => Set(ref _proxyEnabled, value);
    }

    public string ProxyHost
    {
        get => _proxyHost;
        set => Set(ref _proxyHost, value);
    }

    public string ProxyPort
    {
        get => _proxyPort;
        set => Set(ref _proxyPort, value);
    }

    public bool IsDetecting
    {
        get => _isDetecting;
        private set => Set(ref _isDetecting, value);
    }

    public RelayCommand SaveCommand { get; }
    public RelayCommand BrowseGameDirectoryCommand { get; }
    public RelayCommand BrowseJavaPathCommand { get; }
    public RelayCommand DetectJavaCommand { get; }

    public event Action? Saved;

    public SettingsViewModel()
    {
        SaveCommand = new RelayCommand(_ => Save());
        BrowseGameDirectoryCommand = new RelayCommand(_ => BrowseGameDirectory());
        BrowseJavaPathCommand = new RelayCommand(_ => BrowseJavaPath());
        DetectJavaCommand = new RelayCommand(async _ => await DetectJavaAsync(), _ => !IsDetecting);
    }

    private void Save()
    {
        if (!Directory.Exists(GameDirectory))
            Directory.CreateDirectory(GameDirectory);

        ConfigService.Current.GameDirectory = GameDirectory;
        ConfigService.Current.JavaPath = JavaPath;
        ConfigService.Current.ProxyEnabled = ProxyEnabled;
        ConfigService.Current.ProxyHost = ProxyHost;
        if (int.TryParse(ProxyPort, out var port) && port is > 0 and <= 65535)
            ConfigService.Current.ProxyPort = port;

        ConfigService.Save();
        HttpService.ConfigureProxy();
        LogManager.Instance.Info("设置已保存（代理配置已应用）");
        Saved?.Invoke();
    }

    private async Task DetectJavaAsync()
    {
        IsDetecting = true;
        JavaStatus = "正在检测...";
        try
        {
            var java = await Task.Run(JavaLocator.FindBestJava);
            if (java is null)
            {
                JavaStatus = "未检测到 Java，请手动指定路径";
                LogManager.Instance.Warn("Java 自动检测未找到可用运行时");
            }
            else
            {
                JavaPath = java;
                JavaStatus = $"已找到 Java {JavaLocator.GetMajorVersion(java)}: {java}";
                LogManager.Instance.Info($"Java 自动检测: {java}");
            }
        }
        finally
        {
            IsDetecting = false;
        }
    }

    private void BrowseGameDirectory()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择游戏工作目录",
            CheckFileExists = false,
            CheckPathExists = false,
            ValidateNames = false,
            FileName = "选择此文件夹",
            InitialDirectory = Directory.Exists(GameDirectory) ? GameDirectory : null
        };
        if (dialog.ShowDialog() == true && Path.GetDirectoryName(dialog.FileName) is { } folder)
            GameDirectory = folder;
    }

    private void BrowseJavaPath()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择 java.exe",
            Filter = "Java 可执行文件|java.exe|所有文件|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog() == true)
            JavaPath = dialog.FileName;
    }
}
