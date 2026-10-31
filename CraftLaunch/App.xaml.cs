using CraftLaunch.Services;
using System.Windows;

namespace CraftLaunch;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ConfigService.Load();
        HttpService.ConfigureProxy();
        ThemeManager.Apply(ConfigService.Current.Theme);
        LogManager.Instance.Info("CraftLaunch 已启动");
    }
}
