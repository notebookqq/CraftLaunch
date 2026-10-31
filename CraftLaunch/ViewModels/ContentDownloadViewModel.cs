using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using CraftLaunch.Models;
using CraftLaunch.Services;

namespace CraftLaunch.ViewModels;

public class ContentDownloadViewModel : ViewModelBase
{
    private const string NoFilter = "不限版本";

    private readonly ModService _modService = new();
    private readonly ModrinthService _modrinth = new();
    private readonly RemoteVersionService _remoteService = new();

    private string _searchText = "";
    private string _selectedGameVersion = NoFilter;
    private string _selectedLoader = "不限";
    private ModrinthProject? _selectedProject;
    private ModInfo? _selectedLocalFile;
    private bool _isSearching;
    private bool _isDownloading;
    private string _downloadStatus = "";
    private string _localSummary = "";

    public bool IsModMode { get; }

    public ContentDownloadViewModel(bool isModMode)
    {
        IsModMode = isModMode;
        SearchCommand = new RelayCommand(async _ => await SearchAsync(), _ => !IsSearching && !IsDownloading);
        DownloadCommand = new RelayCommand(async _ => await DownloadAsync(),
            _ => SelectedProject is not null && !IsDownloading && !IsSearching);
        RefreshLocalCommand = new RelayCommand(_ => RefreshLocal());
        DeleteLocalCommand = new RelayCommand(_ => DeleteLocal(), _ => SelectedLocalFile is not null);
        RefreshLocal();
        _ = LoadGameVersionsAsync();
    }

    public bool SupportsLoader => IsModMode;

    public string TypeLabel => IsModMode ? "模组" : "光影";

    public string SearchText
    {
        get => _searchText;
        set => Set(ref _searchText, value);
    }

    public ObservableCollection<string> Loaders { get; } = new() { "不限", "fabric", "forge", "quilt", "neoforge" };

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

    public ObservableCollection<ModInfo> LocalFiles { get; } = new();

    public ModInfo? SelectedLocalFile
    {
        get => _selectedLocalFile;
        set => Set(ref _selectedLocalFile, value);
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

    public string LocalSummary
    {
        get => _localSummary;
        private set => Set(ref _localSummary, value);
    }

    public RelayCommand SearchCommand { get; }
    public RelayCommand DownloadCommand { get; }
    public RelayCommand RefreshLocalCommand { get; }
    public RelayCommand DeleteLocalCommand { get; }

    private string TargetFolder => IsModMode ? "mods" : "shaderpacks";

    public void RefreshLocal()
    {
        var gamePath = ConfigService.Current.GameDirectory;
        var task = IsModMode ? _modService.GetModsAsync(gamePath) : _modService.GetShadersAsync(gamePath);
        task.ContinueWith(t =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (t.IsFaulted)
                {
                    LogManager.Instance.Error("本地文件扫描失败");
                    return;
                }
                LocalFiles.Clear();
                foreach (var file in t.Result)
                    LocalFiles.Add(file);
                LocalSummary = $"本地共 {LocalFiles.Count} 个{TypeLabel}（{gamePath}\\{TargetFolder}）";
            });
        });
    }

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
            var loader = SupportsLoader && SelectedLoader != "不限" ? SelectedLoader : null;
            var gameVersion = SelectedGameVersion == NoFilter ? null : SelectedGameVersion;
            var results = await _modrinth.SearchAsync(SearchText, IsModMode ? "mod" : "shader",
                gameVersion, loader, CancellationToken.None);

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
            LogManager.Instance.Error($"Modrinth 搜索{TypeLabel}失败: {ex.Message}");
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
        DownloadStatus = $"正在解析 {project.Title} 的可用版本...";
        try
        {
            var versions = await _modrinth.GetVersionsAsync(project.ProjectId, CancellationToken.None);
            var file = PickFile(versions, gameVersion);
            if (file is null)
            {
                DownloadStatus = $"没有适配 {gameVersion ?? "任意版本"} 的文件，请更换版本筛选";
                return;
            }

            var targetDir = Path.Combine(ConfigService.Current.GameDirectory, TargetFolder);
            var item = new DownloadItem
            {
                Url = file.Url,
                Destination = Path.Combine(targetDir, PathGuard.SanitizeFileName(file.Filename)),
                Size = file.Size,
                Display = file.Filename
            };

            DownloadStatus = $"正在下载 {file.Filename} ...";
            await VersionInstaller.DownloadFileAsync(item, CancellationToken.None);
            LogManager.Instance.Info($"已下载{TypeLabel} {file.Filename} 到 {targetDir}");
            DownloadStatus = $"已下载 {file.Filename}";
            RefreshLocal();
        }
        catch (Exception ex)
        {
            DownloadStatus = $"下载失败: {ex.Message}";
            LogManager.Instance.Error($"下载{TypeLabel}失败: {ex.Message}");
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

    private void DeleteLocal()
    {
        if (SelectedLocalFile is null)
            return;

        var file = SelectedLocalFile;
        var confirm = MessageBox.Show($"确定要删除 {file.Name} 吗？", "删除文件",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes)
            return;

        try
        {
            File.Delete(file.Path);
            LogManager.Instance.Warn($"已删除: {file.Path}");
            RefreshLocal();
        }
        catch (Exception ex)
        {
            LogManager.Instance.Error($"删除失败: {ex.Message}");
        }
    }
}
