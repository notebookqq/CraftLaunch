using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using CraftLaunch.Models;
using CraftLaunch.Services;

namespace CraftLaunch.ViewModels;

public class VersionDownloadViewModel : ViewModelBase
{
    private readonly RemoteVersionService _remoteService = new();
    private readonly VersionInstaller _installer = new();
    private List<RemoteVersionInfo> _allRemote = new();
    private CancellationTokenSource? _downloadCts;

    public ObservableCollection<RemoteVersionInfo> RemoteVersions { get; } = new();

    private RemoteVersionInfo? _selectedRemoteVersion;

    public RemoteVersionInfo? SelectedRemoteVersion
    {
        get => _selectedRemoteVersion;
        set => Set(ref _selectedRemoteVersion, value);
    }

    private bool _showSnapshots;

    public bool ShowSnapshots
    {
        get => _showSnapshots;
        set
        {
            if (Set(ref _showSnapshots, value))
                RefillRemote();
        }
    }

    private string _versionFilter = "";

    public string VersionFilter
    {
        get => _versionFilter;
        set
        {
            if (Set(ref _versionFilter, value))
                RefillRemote();
        }
    }

    private bool _isLoadingRemote;

    public bool IsLoadingRemote
    {
        get => _isLoadingRemote;
        private set
        {
            if (Set(ref _isLoadingRemote, value))
            {
                OnPropertyChanged(nameof(IsNotBusy));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool IsNotBusy => !IsDownloading && !IsLoadingRemote;

    private bool _isDownloading;

    public bool IsDownloading
    {
        get => _isDownloading;
        private set
        {
            if (Set(ref _isDownloading, value))
            {
                OnPropertyChanged(nameof(IsNotBusy));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    private string _downloadStatus = "";

    public string DownloadStatus
    {
        get => _downloadStatus;
        private set => Set(ref _downloadStatus, value);
    }

    private double _downloadPercent;

    public double DownloadPercent
    {
        get => _downloadPercent;
        private set => Set(ref _downloadPercent, value);
    }

    public RelayCommand LoadRemoteCommand { get; }
    public RelayCommand DownloadCommand { get; }
    public RelayCommand CancelDownloadCommand { get; }

    public event Action? VersionInstalled;

    public VersionDownloadViewModel()
    {
        LoadRemoteCommand = new RelayCommand(async _ => await LoadRemoteAsync(), _ => !IsLoadingRemote && !IsDownloading);
        DownloadCommand = new RelayCommand(async _ => await DownloadAsync(),
            _ => SelectedRemoteVersion is not null && !IsDownloading && !IsLoadingRemote);
        CancelDownloadCommand = new RelayCommand(_ => _downloadCts?.Cancel(), _ => IsDownloading);
        _ = LoadRemoteAsync();
    }

    private async Task LoadRemoteAsync()
    {
        IsLoadingRemote = true;
        DownloadStatus = "正在获取在线版本列表...";
        try
        {
            _allRemote = await _remoteService.GetVersionsAsync();
            RefillRemote();
            DownloadStatus = $"在线版本列表已就绪（{_allRemote.Count} 个版本）";
            LogManager.Instance.Info($"在线版本列表已获取，共 {_allRemote.Count} 个版本");
        }
        catch (Exception ex)
        {
            DownloadStatus = "在线版本列表获取失败";
            LogManager.Instance.Warn($"在线版本列表获取失败: {ex.Message}（可点击刷新重试，如需代理请在设置中配置）");
        }
        finally
        {
            IsLoadingRemote = false;
        }
    }

    private void RefillRemote()
    {
        var keyword = _versionFilter.Trim();
        var visible = _allRemote
            .Where(v => (ShowSnapshots || v.Type == "release") &&
                        (keyword.Length == 0 || v.Id.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        RemoteVersions.Clear();
        foreach (var version in visible)
            RemoteVersions.Add(version);

        if (SelectedRemoteVersion is not null && !RemoteVersions.Contains(SelectedRemoteVersion))
            SelectedRemoteVersion = null;

        if (!IsDownloading)
            DownloadStatus = $"共 {RemoteVersions.Count} 个版本，选中后点击下载";
    }

    private async Task DownloadAsync()
    {
        if (SelectedRemoteVersion is null)
            return;

        var remote = SelectedRemoteVersion;
        _downloadCts = new CancellationTokenSource();
        var progress = new Progress<DownloadProgress>(p =>
        {
            DownloadPercent = p.Percent;
            DownloadStatus = $"{p.Phase}（{p.CompletedFiles}/{p.TotalFiles}）{p.CurrentFile}";
        });

        IsDownloading = true;
        DownloadPercent = 0;
        LogManager.Instance.Info($"开始下载版本 {remote.Id} ...");
        try
        {
            await _installer.InstallAsync(ConfigService.Current.GameDirectory, remote, progress, _downloadCts.Token);
            DownloadStatus = $"版本 {remote.Id} 下载完成";
            VersionInstalled?.Invoke();
        }
        catch (OperationCanceledException)
        {
            DownloadStatus = "下载已取消";
            LogManager.Instance.Warn($"版本 {remote.Id} 下载已取消");
        }
        catch (Exception ex)
        {
            DownloadStatus = $"下载失败: {ex.Message}";
            LogManager.Instance.Error($"版本 {remote.Id} 下载失败: {ex.Message}");
        }
        finally
        {
            IsDownloading = false;
            _downloadCts.Dispose();
            _downloadCts = null;
        }
    }
}
