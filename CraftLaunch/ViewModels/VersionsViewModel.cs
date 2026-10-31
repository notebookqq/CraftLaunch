using System.Collections.ObjectModel;
using System.Windows;
using CraftLaunch.Models;
using CraftLaunch.Services;

namespace CraftLaunch.ViewModels;

public class VersionsViewModel : ViewModelBase
{
    private readonly IVersionProvider _versionProvider = new VersionService();

    public ObservableCollection<McVersion> Versions { get; } = new();

    private McVersion? _selectedVersion;

    public McVersion? SelectedVersion
    {
        get => _selectedVersion;
        set => Set(ref _selectedVersion, value);
    }

    private string _summary = "";

    public string Summary
    {
        get => _summary;
        private set => Set(ref _summary, value);
    }

    public RelayCommand RefreshCommand { get; }
    public RelayCommand DeleteCommand { get; }

    public VersionsViewModel()
    {
        RefreshCommand = new RelayCommand(_ => Load());
        DeleteCommand = new RelayCommand(_ => Delete(), _ => SelectedVersion is not null);
        Load();
    }

    public void Refresh() => Load();

    private void Load()
    {
        var gamePath = ConfigService.Current.GameDirectory;
        _versionProvider.GetVersionsAsync(gamePath).ContinueWith(t =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (t.IsFaulted)
                {
                    LogManager.Instance.Error($"版本扫描失败: {t.Exception?.GetBaseException().Message}");
                    return;
                }
                Versions.Clear();
                foreach (var version in t.Result)
                    Versions.Add(version);
                Summary = $"本地共 {Versions.Count} 个版本";
            });
        });
    }

    private void Delete()
    {
        if (SelectedVersion is null)
            return;

        var version = SelectedVersion;
        var confirm = MessageBox.Show($"确定要删除版本 {version.Id} 吗？\n该操作会删除其目录下的所有文件。", "删除版本",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes)
            return;

        try
        {
            _versionProvider.Delete(version);
            LogManager.Instance.Warn($"已删除版本: {version.Id}");
            Load();
        }
        catch (Exception ex)
        {
            LogManager.Instance.Error($"删除版本失败: {ex.Message}");
        }
    }
}
