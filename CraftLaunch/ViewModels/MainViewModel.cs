using System.Collections.ObjectModel;

namespace CraftLaunch.ViewModels;

public class MainViewModel : ViewModelBase
{
    public HomeViewModel Home { get; } = new();
    public VersionsViewModel Versions { get; } = new();
    public DownloadViewModel Downloads { get; } = new();
    public SettingsViewModel Settings { get; } = new();
    public AboutViewModel About { get; } = new();

    public ObservableCollection<NavItem> NavItems { get; } = new()
    {
        new NavItem("Home", "\uE80F", "主页"),
        new NavItem("Versions", "\uE8F1", "版本管理"),
        new NavItem("Downloads", "\uE896", "下载"),
        new NavItem("Settings", "\uE713", "设置"),
        new NavItem("About", "\uE946", "关于"),
    };

    private ViewModelBase _current;

    public ViewModelBase Current
    {
        get => _current;
        set => Set(ref _current, value);
    }

    private string _currentKey = "Home";

    public string CurrentKey
    {
        get => _currentKey;
        set
        {
            if (!Set(ref _currentKey, value))
                return;
            Current = value switch
            {
                "Versions" => Versions,
                "Downloads" => Downloads,
                "Settings" => Settings,
                "About" => About,
                _ => Home
            };
        }
    }

    public MainViewModel()
    {
        _current = Home;
        Settings.Saved += OnSettingsSaved;
        Downloads.Versions.VersionInstalled += OnVersionInstalled;
    }

    private void OnVersionInstalled()
    {
        Home.RefreshVersions();
        Versions.Refresh();
    }

    private void OnSettingsSaved()
    {
        Home.RefreshVersions();
        Downloads.RefreshAll();
    }
}
