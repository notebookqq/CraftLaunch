using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using CraftLaunch.Models;
using CraftLaunch.Services;

namespace CraftLaunch.ViewModels;

public class ModpackDownloadViewModel : ViewModelBase
{
    private const string NoFilter = "不限版本";

    private readonly ModrinthService _modrinth = new();
    private readonly RemoteVersionService _remoteService = new();
    private readonly ModpackInstaller _installer = new();

    private string _searchText = "";
    private string _selectedGameVersion = NoFilter;
    private string _selectedLoader = "不限";
    private ModrinthProject? _selectedProject;
    private bool _isSearching;
    private bool _isDownloading;
    private string _downloadStatus = "";
    private double _downloadPercent;

    public ModpackDownloadViewModel()
    {
        SearchCommand = new RelayCommand(async _ => await SearchAsync(), _ => !IsSearching && !IsDownloading);
        DownloadCommand = new RelayCommand(async _ => await DownloadAsync(),
            _ => SelectedProject is not null && !IsDownloading && !IsSearching);
        _ = LoadGameVersionsAsync();
    }

    public string SearchText
    {
        get => _searchText;
        set => Set(ref _searchText, value);
    }

    public ObservableCollection<string> Loaders { get; } = new() { "不限", "fabric", "forge", "quilt" };

    public string SelectedLoader
    {
        get => _selectedLoader;
        set => Set(ref _selectedLoader, value);
    }

    public ObservableCollection<string> GameVersions { get; } = new();

    public string SelectedGameVersion
    {
        get => _selectedGameVersion;
        set => Set(ref _selectedGameVersion, value);
    }

    public ObservableCollection<ModrinthProject> SearchResults { get; } = new();

    public ModrinthProject? SelectedProject
    {
        get => _selectedProject;
        set => Set(ref _selectedProject, value);
    }

    public bool IsSearching
    {
        get => _isSearching;
        private set
        {
            if (Set(ref _isSearching, value))
                CommandManager.InvalidateRequerySuggested();
        }
    }

    public bool IsDownloading
    {
        get => _isDownloading;
        private set
        {
            if (Set(ref _isDownloading, value))
                CommandManager.InvalidateRequerySuggested();
        }
    }

    public string DownloadStatus
    {
        get => _downloadStatus;
        private set => Set(ref _downloadStatus, value);
    }

    public double DownloadPercent
    {
        get => _downloadPercent;
        private set => Set(ref _downloadPercent, value);
    }

    public RelayCommand SearchCommand { get; }
    public RelayCommand DownloadCommand { get; }

    private async Task LoadGameVersionsAsync()
    {
        try
        {
            var versions = await _remoteService.GetVersionsAsync();
            var releases = versions.Where(v => v.Type == "release").Select(v => v.Id).ToList();
            Application.Current.Dispatcher.Invoke(() =>
            {
                GameVersions.Clear();
                GameVersions.Add(NoFilter);
                foreach (var id in releases)
                    GameVersions.Add(id);
                if (!GameVersions.Contains(SelectedGameVersion))
                    SelectedGameVersion = NoFilter;
            });
        }
        catch
        {
            // 版本筛选列表获取失败时允许不筛选
        }
    }

    public async Task SearchAsync()
    {
        IsSearching = true;
        DownloadStatus = "正在搜索...";
        try
        {
            var loader = SelectedLoader != "不限" ? SelectedLoader : null;
            var gameVersion = SelectedGameVersion == NoFilter ? null : SelectedGameVersion;
            var results = await _modrinth.SearchAsync(SearchText, "modpack", gameVersion, loader, CancellationToken.None);

            Application.Current.Dispatcher.Invoke(() =>
            {
                SearchResults.Clear();
                foreach (var project in results)
                    SearchResults.Add(project);
                SelectedProject = null;
                DownloadStatus = $"找到 {SearchResults.Count} 个结果";
            });
        }
        catch (Exception ex)
        {
            DownloadStatus = $"搜索失败: {ex.Message}";
            LogManager.Instance.Error($"Modrinth 搜索整合包失败: {ex.Message}");
        }
        finally
        {
            IsSearching = false;
        }
    }

    private async Task DownloadAsync()
    {
        if (SelectedProject is null)
            return;

        var project = SelectedProject;
        var gameVersion = SelectedGameVersion == NoFilter ? null : SelectedGameVersion;

        IsDownloading = true;
        DownloadPercent = 0;
        DownloadStatus = $"正在解析 {project.Title} 的可用版本...";
        try
        {
            var versions = await _modrinth.GetVersionsAsync(project.ProjectId, CancellationToken.None);
            var file = PickFile(versions, gameVersion);
            if (file is null)
            {
                DownloadStatus = $"没有适配 {gameVersion ?? "任意版本"} 的整合包文件，请更换版本筛选";
                return;
            }

            var gameDir = ConfigService.Current.GameDirectory;
            var packDir = Path.Combine(gameDir, "modpacks");
            var mrpackPath = Path.Combine(packDir, PathGuard.SanitizeFileName(file.Filename));
            await VersionInstaller.DownloadFileAsync(new DownloadItem
            {
                Url = file.Url,
                Destination = mrpackPath,
                Size = file.Size,
                Display = file.Filename
            }, CancellationToken.None);

            LogManager.Instance.Info($"整合包 {file.Filename} 已下载，开始安装...");
            DownloadStatus = $"正在安装 {file.Filename} ...";
            var progress = new Progress<DownloadProgress>(p =>
            {
                DownloadPercent = p.Percent;
                DownloadStatus = $"{p.Phase}（{p.CompletedFiles}/{p.TotalFiles}）{p.CurrentFile}";
            });

            await _installer.InstallAsync(mrpackPath, gameDir, progress, CancellationToken.None);
            DownloadStatus = $"整合包 {project.Title} 安装完成，可在主页选择对应版本启动";
            LogManager.Instance.Info($"整合包 {project.Title} 安装完成");
        }
        catch (Exception ex)
        {
            DownloadStatus = $"下载失败: {ex.Message}";
            LogManager.Instance.Error($"整合包下载/安装失败: {ex.Message}");
        }
        finally
        {
            IsDownloading = false;
        }
    }

    private static ModrinthFile? PickFile(List<ModrinthVersion> versions, string? gameVersion)
    {
        IEnumerable<ModrinthVersion> candidates = versions.OrderByDescending(v => v.DatePublished);
        if (!string.IsNullOrEmpty(gameVersion))
        {
            candidates = candidates.Where(v => v.GameVersions.Contains(gameVersion)).ToList();
        }
        var best = candidates.FirstOrDefault();
        return best?.Files.OrderByDescending(f => f.Primary).FirstOrDefault();
    }
}
