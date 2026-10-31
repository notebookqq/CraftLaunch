using System.Collections.ObjectModel;
using CraftLaunch.ViewModels;

namespace CraftLaunch.ViewModels;

public class DownloadViewModel : ViewModelBase
{
    public VersionDownloadViewModel Versions { get; } = new();
    public ContentDownloadViewModel Mods { get; } = new(true);
    public ContentDownloadViewModel Shaders { get; } = new(false);
    public ModpackDownloadViewModel Modpacks { get; } = new();

    public ObservableCollection<string> Tabs { get; } = new()
    {
        "下载版本",
        "下载模组",
        "下载光影",
        "下载整合包",
    };

    private int _selectedTab;
    private bool _modsSearched;
    private bool _shadersSearched;
    private bool _modpacksSearched;

    public int SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (!Set(ref _selectedTab, value))
                return;
            AutoSearch(value);
            OnPropertyChanged(nameof(Current));
        }
    }

    private void AutoSearch(int tab)
    {
        if (tab == 1 && !_modsSearched)
        {
            _modsSearched = true;
            _ = Mods.SearchAsync();
        }
        else if (tab == 2 && !_shadersSearched)
        {
            _shadersSearched = true;
            _ = Shaders.SearchAsync();
        }
        else if (tab == 3 && !_modpacksSearched)
        {
            _modpacksSearched = true;
            _ = Modpacks.SearchAsync();
        }
    }

    public ViewModelBase Current => SelectedTab switch
    {
        0 => Versions,
        1 => Mods,
        2 => Shaders,
        _ => Modpacks
    };

    public void RefreshAll()
    {
        Mods.RefreshLocal();
        Shaders.RefreshLocal();
    }
}
